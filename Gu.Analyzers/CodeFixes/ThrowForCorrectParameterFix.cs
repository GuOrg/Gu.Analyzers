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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ThrowForCorrectParameterFix))]
    [Shared]
    internal class ThrowForCorrectParameterFix : DocumentEditorCodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            GU0013CheckNameInThrow.DiagnosticId);

        /// <inheritdoc/>
        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNode(diagnostic, out ArgumentSyntax argument) &&
                    diagnostic.Properties.TryGetValue("Name", out var name))
                {
                    context.RegisterCodeFix(
                        "Use correct parameter name.",
                        (editor, _) => editor.ReplaceNode(
                            argument.Expression,
                            CreateNode()),
                        this.GetType(),
                        diagnostic);

                    ExpressionSyntax CreateNode()
                    {
                        return argument.Parent is ArgumentListSyntax argumentList &&
                               argumentList.Parent is InvocationExpressionSyntax invocation &&
                               invocation.IsNameOf()
                            ? SyntaxFactory.IdentifierName(name)
                            : SyntaxFactory.ParseExpression($"nameof({name})");
                    }
                }
            }
        }
    }
}
