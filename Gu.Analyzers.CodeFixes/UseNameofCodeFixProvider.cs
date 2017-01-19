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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseNameofCodeFixProvider))]
    [Shared]
    internal class UseNameofCodeFixProvider : CodeFixProvider
    {
        private static readonly IdentifierNameSyntax NameofIdentifier = SyntaxFactory.IdentifierName(@"nameof");

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(GU0006UseNameof.DiagnosticId);

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
                if (string.IsNullOrEmpty(token.ValueText) || token.IsMissing)
                {
                    continue;
                }

                var argument = (ArgumentSyntax)syntaxRoot.FindNode(diagnostic.Location.SourceSpan);

                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Use nameof",
                        _ => ApplyFixAsync(context, syntaxRoot, semanticModel, argument, !diagnostic.Properties.IsEmpty),
                        nameof(UseNameofCodeFixProvider)),
                    diagnostic);
            }
        }

        private static Task<Document> ApplyFixAsync(CodeFixContext context, SyntaxNode syntaxRoot, SemanticModel semanticModel, ArgumentSyntax argument, bool isMember)
        {
            var text = ((LiteralExpressionSyntax)argument.Expression).Token.ValueText;
            var identifierNameSyntax = SyntaxFactory.IdentifierName(text);
            var expression = isMember && !argument.UsesUnderscoreNames(semanticModel, context.CancellationToken)
                ? SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ThisExpression(),
                    identifierNameSyntax)
                : (ExpressionSyntax)identifierNameSyntax;

            var argumentList = SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(expression)));
            var nameofInvocation = SyntaxFactory.InvocationExpression(NameofIdentifier, argumentList);
            return Task.FromResult(context.Document.WithSyntaxRoot(syntaxRoot.ReplaceNode(argument, argument.WithExpression(nameofInvocation))));
        }
    }
}