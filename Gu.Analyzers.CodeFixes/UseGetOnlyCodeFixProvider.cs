namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseGetOnlyCodeFixProvider))]
    [Shared]
    internal class UseGetOnlyCodeFixProvider : CodeFixProvider
    {
        private static readonly AccessorListSyntax GetOnlyAccessorList =
            SyntaxFactory.AccessorList(
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                SyntaxFactory.SingletonList(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                 .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))),
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken));

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(GU0021CalculatedPropertyAllocates.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                 .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText))
                {
                    continue;
                }

                var arrow = (ArrowExpressionClauseSyntax)syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                var objectCreation = (ObjectCreationExpressionSyntax)arrow.Expression;
                var arguments = objectCreation.ArgumentList.Arguments;
                bool hasMutable = IsAnyArgumentMutable(semanticModel, arguments) ||
                                  IsAnyInitializerMutable(semanticModel, objectCreation.Initializer);

                var property = (PropertyDeclarationSyntax)arrow.Parent;
                ConstructorDeclarationSyntax ctor;
                if (TryGetConstructor(property, out ctor))
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Use get-only" + (hasMutable ? " UNSAFE" : string.Empty),
                            _ => ApplyFixAsync(context, syntaxRoot, ctor, property),
                            nameof(UseGetOnlyCodeFixProvider)),
                        diagnostic);
                }
            }
        }

        private static Task<Document> ApplyFixAsync(CodeFixContext context, SyntaxNode syntaxRoot, ConstructorDeclarationSyntax ctor, PropertyDeclarationSyntax property)
        {
            var member = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.ThisExpression(),
                SyntaxFactory.Token(SyntaxKind.DotToken),
                SyntaxFactory.IdentifierName(property.Identifier));
            var assignment =
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                                     SyntaxKind.SimpleAssignmentExpression,
                                     member,
                                     property.ExpressionBody.Expression));
            syntaxRoot = syntaxRoot.TrackNodes(ctor.Body, property);
            var updatedBody = ctor.Body.AddStatements(assignment);
            syntaxRoot = syntaxRoot.ReplaceNode(syntaxRoot.GetCurrentNode(ctor.Body), updatedBody);
            var trackedProperty = syntaxRoot.GetCurrentNode(property);
            var updatedProperty = trackedProperty.WithExpressionBody(null)
                                                 .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                                                 .WithAccessorList(GetOnlyAccessorList);
            syntaxRoot = syntaxRoot.ReplaceNode(trackedProperty, updatedProperty);
            return Task.FromResult(context.Document.WithSyntaxRoot(syntaxRoot));
        }

        private static bool IsAnyInitializerMutable(SemanticModel semanticModel, InitializerExpressionSyntax initializer)
        {
            if (initializer == null)
            {
                return false;
            }

            foreach (var expression in initializer.Expressions)
            {
                var assignment = expression as AssignmentExpressionSyntax;
                if (assignment == null)
                {
                    // Don't know if this can ever happen but erroring on the safe side flagging it as mutable.
                    return true;
                }

                if (IsMutable(semanticModel, assignment.Right))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAnyArgumentMutable(SemanticModel semanticModel, SeparatedSyntaxList<ArgumentSyntax> arguments)
        {
            foreach (var argument in arguments)
            {
                if (IsMutable(semanticModel, argument.Expression))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsMutable(SemanticModel semanticModel, ExpressionSyntax expression)
        {
            if (expression is LiteralExpressionSyntax || expression is ThisExpressionSyntax || expression is ParenthesizedLambdaExpressionSyntax)
            {
                return false;
            }

            var symbol = semanticModel.GetSymbolInfo(expression)
                                      .Symbol;
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

            var memberAccess = expression as MemberAccessExpressionSyntax;
            if (memberAccess != null)
            {
                return IsMutable(semanticModel, memberAccess.Expression);
            }

            return true;
        }

        private static bool TryGetConstructor(PropertyDeclarationSyntax property, out ConstructorDeclarationSyntax result)
        {
            result = null;
            foreach (var member in ((TypeDeclarationSyntax)property.Parent).Members)
            {
                var ctor = member as ConstructorDeclarationSyntax;
                if (ctor != null)
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
