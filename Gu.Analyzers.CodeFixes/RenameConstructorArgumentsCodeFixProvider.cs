namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RenameConstructorArgumentsCodeFixProvider))]
    [Shared]
    internal class RenameConstructorArgumentsCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(GU0003CtorParameterNamesShouldMatch.DiagnosticId);

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

                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Rename parameter",
                        cancellationToken => ApplyFixAsync(cancellationToken, context, diagnostic, semanticModel, syntaxRoot, token),
                        nameof(NameArgumentsCodeFixProvider)),
                    diagnostic);
            }
        }

        private static Task<Solution> ApplyFixAsync(CancellationToken cancellationToken, CodeFixContext context, Diagnostic diagnostic, SemanticModel semanticModel, SyntaxNode syntaxRoot, SyntaxToken token)
        {
            var parameter = syntaxRoot.FindNode(diagnostic.Location.SourceSpan)
                                      .FirstAncestorOrSelf<ParameterSyntax>();
            var constructorDeclarationSyntax = parameter.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
            using (var walker = ConstructorWalker.Create(constructorDeclarationSyntax, semanticModel, context.CancellationToken))
            {
                foreach (var kvp in walker.parameterNameMap)
                {
                    if (kvp.Key == parameter)
                    {
                        return RenameHelper.RenameSymbolAsync(
                            context.Document,
                            syntaxRoot,
                            token,
                            kvp.Value,
                            cancellationToken);
                    }
                }
            }

            // should never get here.
            return Task.FromResult(context.Document.Project.Solution);
        }
    }
}