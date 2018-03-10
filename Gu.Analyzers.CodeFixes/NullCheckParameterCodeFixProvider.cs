namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullCheckParameterCodeFixProvider))]
    [Shared]
    internal class NullCheckParameterCodeFixProvider : DocumentEditorCodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            GU0012NullCheckParameter.DiagnosticId);

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var identifier = (IdentifierNameSyntax)syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                context.RegisterCodeFix(
                    "Throw if null.",
                    (editor, _) => editor.ReplaceNode(
                        identifier,
                        SyntaxFactory.ParseExpression($"{identifier.Identifier.ValueText} ?? throw new System.ArgumentNullException(nameof({identifier.Identifier.ValueText}))").WithSimplifiedNames()),
                    this.GetType(),
                    diagnostic);
            }
        }
    }
}