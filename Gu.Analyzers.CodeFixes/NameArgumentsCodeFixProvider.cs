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
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NameArgumentsCodeFixProvider))]
    [Shared]
    internal class NameArgumentsCodeFixProvider : CodeFixProvider
    {
        private static readonly SyntaxTriviaList SpaceTrivia = SyntaxTriviaList.Empty.Add(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " "));

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(GU0001NameArguments.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => DocumentEditorFixAllProvider.Default;

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

                var arguments = (ArgumentListSyntax)syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                if (HasAnyNamedArgument(arguments))
                {
                    continue;
                }

                var method = semanticModel.GetSymbolSafe(arguments.Parent, context.CancellationToken) as IMethodSymbol;
                if (method == null)
                {
                    continue;
                }

                context.RegisterDocumentEditorFix(
                        "Name arguments",
                        (editor, _) => ApplyFix(editor, method, arguments),
                        this.GetType(),
                    diagnostic);
            }
        }

        private static void ApplyFix(DocumentEditor editor, IMethodSymbol method, ArgumentListSyntax arguments)
        {
            var withNames = arguments;
            for (int i = 0; i < arguments.Arguments.Count; i++)
            {
                var argument = withNames.Arguments[i];
                var leadingTrivia = argument.GetLeadingTrivia();
                var withNameColon = argument.WithLeadingTrivia(SpaceTrivia).WithNameColon(SyntaxFactory.NameColon(method.Parameters[i].Name)).WithLeadingTrivia(leadingTrivia);
                withNames = withNames.ReplaceNode(argument, withNameColon);
            }

            editor.ReplaceNode(arguments, withNames);
        }

        private static bool HasAnyNamedArgument(ArgumentListSyntax argumentList)
        {
            foreach (var argument in argumentList.Arguments)
            {
                if (argument.NameColon != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
