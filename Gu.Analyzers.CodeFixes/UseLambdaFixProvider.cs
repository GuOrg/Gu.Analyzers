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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseLambdaFixProvider))]
    [Shared]
    internal class UseLambdaFixProvider : DocumentEditorCodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(GU0016PreferLambda.DiagnosticId);

        /// <inheritdoc/>
        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNode<ArgumentSyntax>(diagnostic, out var argument))
                {
                    switch (argument.Expression)
                    {
                        case IdentifierNameSyntax identifierName when argument.TryFirstAncestor<MemberDeclarationSyntax>(out _):
                            context.RegisterCodeFix(
                                "Use lambda.",
                                (editor, _) => editor.ReplaceNode(
                                    identifierName,
                                    (node, __) => GetLambda(node)),
                                "Use lambda.",
                                diagnostic);
                            break;
                    }
                }
            }
        }

        private static SyntaxNode GetLambda(SyntaxNode node)
        {
            if (node.TryFirstAncestor<MemberDeclarationSyntax>(out var ancestor))
            {
                using (var walker = IdentifierTokenWalker.Borrow(ancestor))
                {
                    var name = "x";
                    while (walker.TryFind(name, out _))
                    {
                        name += "_";
                    }

                    return SyntaxFactory.ParseExpression($"{name} => {((IdentifierNameSyntax)node).Identifier.ValueText}({name})");
                }
            }

            return node;
        }
    }
}
