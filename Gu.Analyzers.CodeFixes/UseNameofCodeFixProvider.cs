namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading;
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
                        _ => ApplyFixAsync(context, syntaxRoot, argument),
                        nameof(UseNameofCodeFixProvider)),
                    diagnostic);
            }
        }

        private static Task<Document> ApplyFixAsync(CodeFixContext context, SyntaxNode syntaxRoot, ArgumentSyntax argument)
        {
            var text = ((LiteralExpressionSyntax)argument.Expression).Token.ValueText;
            var identifierNameSyntax = SyntaxFactory.IdentifierName(text);
            var expression = IsMember(argument, text) && !argument.UsesUnderscoreNames()
                ? SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ThisExpression(),
                    identifierNameSyntax)
                : (ExpressionSyntax)identifierNameSyntax;

            var argumentList = SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(expression)));
            var nameofInvocation = SyntaxFactory.InvocationExpression(NameofIdentifier, argumentList);
            var nameofArgument = SyntaxFactory.Argument(nameofInvocation);
            return Task.FromResult(context.Document.WithSyntaxRoot(syntaxRoot.ReplaceNode(argument, nameofArgument)));
        }

        private static bool IsMember(ArgumentSyntax argument, string text)
        {
            var parameterers = argument.FirstAncestorOrSelf<MethodDeclarationSyntax>()
                                              ?.ParameterList ??
                                      argument.FirstAncestorOrSelf<ConstructorDeclarationSyntax>()
                                              ?.ParameterList;
            if (parameterers != null)
            {
                foreach (var parameterer in parameterers.Parameters)
                {
                    if (parameterer.Identifier.ValueText == text)
                    {
                        return false;
                    }
                }
            }

            var typeDeclaration = argument.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            if (typeDeclaration == null)
            {
                return false;
            }

            foreach (var member in typeDeclaration.Members)
            {
                var field = member as BaseFieldDeclarationSyntax;
                if (field != null)
                {
                    foreach (var variable in field.Declaration.Variables)
                    {
                        if (variable.Identifier.ValueText == text)
                        {
                            return true;
                        }
                    }
                }

                var property = member as PropertyDeclarationSyntax;
                if (property?.Identifier.ValueText == text)
                {
                    return true;
                }

                var method = member as MethodDeclarationSyntax;
                if (method?.Identifier.ValueText == text)
                {
                    return true;
                }
            }

            return false;
        }
    }
}