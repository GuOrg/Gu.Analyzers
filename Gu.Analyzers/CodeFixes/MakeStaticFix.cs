namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeStaticFix))]
    [Shared]
    internal class MakeStaticFix : DocumentEditorCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            "CS0708",
            "CA1052");

        protected override DocumentEditorFixAllProvider FixAllProvider() => DocumentEditorFixAllProvider.Project;

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out MethodDeclarationSyntax? methodDeclaration))
                {
                    context.RegisterCodeFix(
                        "Make Static.",
                        (editor, _) => editor.ReplaceNode(
                            methodDeclaration,
                            x => x.WithModifiers(MakeStatic(x.Modifiers))),
                        "Make Static.",
                        diagnostic);
                }
                else if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out ClassDeclarationSyntax? classDeclaration))
                {
                    context.RegisterCodeFix(
                        "Make Static.",
                        (editor, _) => editor.ReplaceNode(
                            classDeclaration,
                            x => x.WithModifiers(MakeStatic(x.Modifiers))),
                        "Make Static.",
                        diagnostic);
                }
            }
        }

        private static SyntaxTokenList MakeStatic(SyntaxTokenList before)
        {
            if (before.TryFirst(out var first))
            {
                switch (first.Kind())
                {
                    case SyntaxKind.PublicKeyword:
                    case SyntaxKind.InternalKeyword:
                    case SyntaxKind.ProtectedKeyword:
                    case SyntaxKind.PrivateKeyword:
                        return before.Insert(1, SyntaxFactory.Token(SyntaxKind.StaticKeyword));
                    default:
                        return before.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
                }
            }

            return SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
        }
    }
}
