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
            Descriptors.GU0021CalculatedPropertyAllocates.Id,
            Descriptors.GU0022UseGetOnly.Id);

        /// <inheritdoc/>
        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                 .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (diagnostic.Id == Descriptors.GU0021CalculatedPropertyAllocates.Id &&
                    syntaxRoot.TryFindNode(diagnostic, out ObjectCreationExpressionSyntax objectCreation) &&
                    objectCreation.TryFirstAncestor(out PropertyDeclarationSyntax property) &&
                    property.Parent is TypeDeclarationSyntax containingType &&
                    containingType.Members.TrySingleOfType(x => !x.Modifiers.Any(SyntaxKind.StaticKeyword), out ConstructorDeclarationSyntax ctor) &&
                    ctor.Body != null)
                {
                    context.RegisterCodeFix(
                        "Use get-only" + (IsUnsafe() ? " UNSAFE" : string.Empty),
                        (editor, cancellationToken) => InitializeInCtorAsync(editor, ctor, property, objectCreation, cancellationToken),
                        this.GetType().FullName + "UNSAFE",
                        diagnostic);

                    bool IsUnsafe()
                    {
                        var arguments = objectCreation.ArgumentList.Arguments;
                        return IsAnyArgumentMutable(semanticModel, arguments, context.CancellationToken) ||
                               IsAnyInitializerMutable(semanticModel, context.CancellationToken, objectCreation.Initializer);
                    }
                }
                else if (diagnostic.Id == Descriptors.GU0022UseGetOnly.Id &&
                         syntaxRoot.TryFindNode(diagnostic, out AccessorDeclarationSyntax setter))
                {
                    context.RegisterCodeFix(
                        "Use get-only",
                        (editor, _) => editor.RemoveNode(setter, SyntaxRemoveOptions.AddElasticMarker),
                        this.GetType(),
                        diagnostic);
                }
            }
        }

        private static async Task InitializeInCtorAsync(
            DocumentEditor editor,
            ConstructorDeclarationSyntax ctor,
            PropertyDeclarationSyntax property,
            ObjectCreationExpressionSyntax objectCreation,
            CancellationToken cancellationToken)
        {
            var qualifyPropertyAccess = await editor.OriginalDocument.QualifyPropertyAccessAsync(cancellationToken).ConfigureAwait(false);
            var member = qualifyPropertyAccess != CodeStyleResult.No
                ? SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ThisExpression(),
                    SyntaxFactory.Token(SyntaxKind.DotToken),
                    SyntaxFactory.IdentifierName(property.Identifier.ValueText))
                : (ExpressionSyntax)SyntaxFactory.IdentifierName(property.Identifier.ValueText);
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
                    if (IsMutable(semanticModel, assignment.Right, cancellationToken))
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

        private static bool IsAnyArgumentMutable(SemanticModel semanticModel, SeparatedSyntaxList<ArgumentSyntax> arguments, CancellationToken cancellationToken)
        {
            foreach (var argument in arguments)
            {
                if (IsMutable(semanticModel, argument.Expression, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsMutable(SemanticModel semanticMode, ExpressionSyntax expression, CancellationToken cancellationToken)
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
    }
}
