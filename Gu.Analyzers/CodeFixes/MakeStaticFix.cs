namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeStaticFix))]
    [Shared]
    internal class MakeStaticFix : DocumentEditorCodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("CS0708");

        protected override DocumentEditorFixAllProvider FixAllProvider() => DocumentEditorFixAllProvider.Project;

        /// <inheritdoc/>
        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out MethodDeclarationSyntax methodDeclaration))
                {
                    context.RegisterCodeFix(
                        "Make Static.",
                        (editor, _) => editor.ReplaceNode(
                            methodDeclaration,
                            x => x.WithModifiers(methodDeclaration.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword)))),
                        "Make Static.",
                        diagnostic);
                }
            }
        }
    }
}
