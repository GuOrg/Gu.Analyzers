namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;

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

            foreach (var diagnostic in context.Diagnostics)
            {
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);

                if (diagnostic.Properties.TryGetValue("Name", out var name))
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Rename parameter",
                            cancellationToken => RenameHelper.RenameSymbolAsync(
                                context.Document,
                                syntaxRoot,
                                token,
                                name,
                                cancellationToken),
                            nameof(NameArgumentsCodeFixProvider)),
                        diagnostic);
                }
            }
        }
    }
}