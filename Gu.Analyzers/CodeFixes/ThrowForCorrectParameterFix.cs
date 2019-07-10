namespace Gu.Analyzers
{
    using System;
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
            GU0013TrowForCorrectParameter.DiagnosticId);

        /// <inheritdoc/>
        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNode(diagnostic, out ArgumentSyntax argument) &&
                    diagnostic.Properties.TryGetValue(nameof(IdentifierNameSyntax), out var name))
                {
                    context.RegisterCodeFix(
                        $"Use nameof({name}).",
                        (editor, _) => editor.ReplaceNode(
                            argument.Expression,
                            x => CreateNode(x).WithTriviaFrom(x)),
                        this.GetType(),
                        diagnostic);

                    ExpressionSyntax CreateNode(ExpressionSyntax old)
                    {
                        switch (old)
                        {
                            case LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression):
                                return SyntaxFactory.ParseExpression($"nameof({name})");
                            case IdentifierNameSyntax identifierName when identifierName.Parent is ArgumentSyntax candidate &&
                                                                          candidate.Parent is ArgumentListSyntax argumentList &&
                                                                          argumentList.Parent is InvocationExpressionSyntax invocation &&
                                                                          invocation.IsNameOf():
                                return identifierName.WithIdentifier(SyntaxFactory.Identifier(name));
                            default:
                                throw new InvalidOperationException("Failed updating parameter name.");
                        }
                    }
                }
            }
        }
    }
}
