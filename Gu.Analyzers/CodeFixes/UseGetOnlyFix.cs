namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseGetOnlyFix))]
    [Shared]
    internal class UseGetOnlyFix : DocumentEditorCodeFixProvider
    {
        private static readonly AccessorListSyntax GetOnlyAccessorList =
            SyntaxFactory.AccessorList(
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                SyntaxFactory.SingletonList(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                 .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))),
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken));

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            GU0021CalculatedPropertyAllocates.Descriptor.Id,
            GU0022UseGetOnly.Descriptor.Id);

        /// <inheritdoc/>
        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                 .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText) || token.IsMissing)
                {
                    continue;
                }

                var syntaxNode = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                if (diagnostic.Id == GU0021CalculatedPropertyAllocates.Descriptor.Id)
                {
                    if (syntaxNode is ObjectCreationExpressionSyntax objectCreation)
                    {
                        var arguments = objectCreation.ArgumentList.Arguments;
                        var hasMutable = IsAnyArgumentMutable(semanticModel, context.CancellationToken, arguments) ||
                                         IsAnyInitializerMutable(semanticModel, context.CancellationToken, objectCreation.Initializer);

                        var property = syntaxNode.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
                        if (TryGetConstructor(property, out var ctor))
                        {
                            context.RegisterCodeFix(
                                "Use get-only" + (hasMutable ? " UNSAFE" : string.Empty),
                                (editor, cancellationToken) =>
                                    ApplyInitializeInCtorFix(editor, ctor, property, objectCreation),
                                this.GetType().FullName + "UNSAFE",
                                diagnostic);
                        }
                    }
                }
                else if (diagnostic.Id == GU0022UseGetOnly.Descriptor.Id)
                {
                    var setter = syntaxNode.FirstAncestorOrSelf<AccessorDeclarationSyntax>();
                    if (setter != null)
                    {
                        context.RegisterCodeFix(
                            "Use get-only",
                            (editor, _) => ApplyRemoveSetterFix(editor, setter),
                            this.GetType(),
                            diagnostic);
                    }
                }
            }
        }

        private static void ApplyRemoveSetterFix(DocumentEditor editor, AccessorDeclarationSyntax setter)
        {
            editor.RemoveNode(setter, SyntaxRemoveOptions.AddElasticMarker);
        }

        private static void ApplyInitializeInCtorFix(
            DocumentEditor editor,
            ConstructorDeclarationSyntax ctor,
            PropertyDeclarationSyntax property,
            ObjectCreationExpressionSyntax objectCreation)
        {
            var member = editor.SemanticModel.UnderscoreFields()
                ? (ExpressionSyntax)SyntaxFactory.IdentifierName(property.Identifier.ValueText)
                : SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ThisExpression(),
                    SyntaxFactory.Token(SyntaxKind.DotToken),
                    SyntaxFactory.IdentifierName(property.Identifier.ValueText));
            var assignment =
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                                     SyntaxKind.SimpleAssignmentExpression,
                                     member,
                                     objectCreation));

            editor.ReplaceNode(ctor.Body, (x, _) => ((BlockSyntax)x).AddStatements(assignment));
            editor.ReplaceNode(
                property,
                (x, _) => ((PropertyDeclarationSyntax)x).WithExpressionBody(null)
                                                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                                                        .WithAccessorList(GetOnlyAccessorList));
        }

        private static bool IsAnyInitializerMutable(SemanticModel semanticModel, CancellationToken cancellationToken, InitializerExpressionSyntax initializer)
        {
            if (initializer == null)
            {
                return false;
            }

            foreach (var expression in initializer.Expressions)
            {
                if (expression is AssignmentExpressionSyntax assignment)
                {
                    if (IsMutable(semanticModel, cancellationToken, assignment.Right))
                    {
                        return true;
                    }
                }
                else
                {
                    // Don't know if this can ever happen but erroring on the safe side flagging it as mutable.
                    return true;
                }
            }

            return false;
        }

        private static bool IsAnyArgumentMutable(SemanticModel semanticModel, CancellationToken cancellationToken, SeparatedSyntaxList<ArgumentSyntax> arguments)
        {
            foreach (var argument in arguments)
            {
                if (IsMutable(semanticModel, cancellationToken, argument.Expression))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsMutable(SemanticModel semanticModel, CancellationToken cancellationToken, ExpressionSyntax expression)
        {
            if (expression is LiteralExpressionSyntax || expression is ThisExpressionSyntax || expression is ParenthesizedLambdaExpressionSyntax)
            {
                return false;
            }

            var symbol = semanticModel.GetSymbolSafe(expression, cancellationToken);
            var property = symbol as IPropertySymbol;
            if (property?.SetMethod != null)
            {
                return true;
            }

            var field = symbol as IFieldSymbol;
            if (field != null && !(field.IsConst || field.IsReadOnly))
            {
                return true;
            }

            if (property == null && field == null)
            {
                return false;
            }

            if (expression is IdentifierNameSyntax)
            {
                return false;
            }

            if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                return IsMutable(semanticModel, cancellationToken, memberAccess.Expression);
            }

            return true;
        }

        private static bool TryGetConstructor(PropertyDeclarationSyntax property, out ConstructorDeclarationSyntax result)
        {
            result = null;
            foreach (var member in ((TypeDeclarationSyntax)property.Parent).Members)
            {
                if (member is ConstructorDeclarationSyntax ctor)
                {
                    if (ctor.Body == null || result != null)
                    {
                        return false;
                    }

                    result = ctor;
                }
            }

            return result != null;
        }
    }
}
