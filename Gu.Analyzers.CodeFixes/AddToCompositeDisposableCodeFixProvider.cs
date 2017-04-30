namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddToCompositeDisposableCodeFixProvider))]
    [Shared]
    internal class AddToCompositeDisposableCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(GU0033DontIgnoreReturnValueOfTypeIDisposable.DiagnosticId);

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText) ||
                    token.IsMissing)
                {
                    continue;
                }

                var node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                if (diagnostic.Id == GU0030DisposeCreated.DiagnosticId)
                {
                    var statement = node.FirstAncestorOrSelf<LocalDeclarationStatementSyntax>();
                    if (statement != null)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Add to CompositeDisposable.",
                                _ => ApplyAddToCompositeDisposableFixAsync(context, statement),
                                nameof(AddToCompositeDisposableCodeFixProvider)),
                            diagnostic);
                    }
                }
            }
        }

        private static async Task<Document> ApplyAddToCompositeDisposableFixAsync(CodeFixContext context, LocalDeclarationStatementSyntax statement)
        {
            var editor = await DocumentEditor.CreateAsync(context.Document).ConfigureAwait(false);
            return editor.OriginalDocument;
        }
    }
}