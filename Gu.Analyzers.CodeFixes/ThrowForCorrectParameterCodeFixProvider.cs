namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ThrowForCorrectParameterCodeFixProvider))]
    [Shared]
    internal class ThrowForCorrectParameterCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            GU0013CheckNameInThrow.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => DocumentEditorFixAllProvider.Default;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var argument = syntaxRoot.FindNode(diagnostic.Location.SourceSpan)
                                           .FirstAncestorOrSelf<ArgumentSyntax>();
                if (argument != null &&
                    diagnostic.Properties.TryGetValue("Name", out var name))
                {
                    context.RegisterDocumentEditorFix(
                        "Throw if null.",
                        (editor, _) => editor.ReplaceNode(
                            argument.Expression,
                            SyntaxFactory.ParseExpression($"nameof({name})")),
                        this.GetType(),
                        diagnostic);
                }
            }
        }
    }
}