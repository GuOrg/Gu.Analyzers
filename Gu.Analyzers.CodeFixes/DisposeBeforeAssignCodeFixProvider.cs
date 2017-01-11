namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DisposeBeforeAssignCodeFixProvider))]
    [Shared]
    internal class DisposeBeforeAssignCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(GU0032DisposeBeforeReassigning.DiagnosticId);

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
                if (string.IsNullOrEmpty(token.ValueText) ||
                    token.IsMissing)
                {
                    continue;
                }

                var assignment = (AssignmentExpressionSyntax)syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                if (!(assignment.Parent is ExpressionStatementSyntax && assignment.Parent.Parent is BlockSyntax))
                {
                    continue;
                }

                StatementSyntax diposeStatement;
                if (TryCreateDisposeStatement(assignment, semanticModel, context.CancellationToken, out diposeStatement))
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Dispose before assigning.",
                            cancellationToken =>
                                    ApplyDisposeBeforeAssignFixAsync(context, syntaxRoot, assignment, diposeStatement),
                            nameof(DisposeBeforeAssignCodeFixProvider)),
                        diagnostic);
                }
            }
        }

        private static Task<Document> ApplyDisposeBeforeAssignFixAsync(CodeFixContext context, SyntaxNode syntaxRoot, AssignmentExpressionSyntax assignment, StatementSyntax disposeStatement)
        {
            var block = assignment.Parent.Parent as BlockSyntax;
            var newBlock = block.InsertNodesBefore(assignment.Parent, new[] { disposeStatement });
            return Task.FromResult(context.Document.WithSyntaxRoot(syntaxRoot.ReplaceNode(block, newBlock)));
        }

        private static bool TryCreateDisposeStatement(AssignmentExpressionSyntax assignment, SemanticModel semanticModel, CancellationToken cancellationToken, out StatementSyntax result)
        {
            var symbol = semanticModel.GetSymbolSafe(assignment.Left, cancellationToken);
            if (symbol == null)
            {
                result = null;
                return false;
            }

            if (!Disposable.IsAssignableTo(MemberType(symbol)))
            {
                result = SyntaxFactory.ParseStatement($"({assignment.Left} as IDisposable)?.Dispose();")
                                    .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                    .WithTrailingTrivia(SyntaxFactory.ElasticMarker);
                return true;
            }

            if (IsAlwaysAssigned(symbol, assignment))
            {
                result = SyntaxFactory.ParseStatement($"{assignment.Left}.Dispose();")
                                    .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                    .WithTrailingTrivia(SyntaxFactory.ElasticMarker);
                return true;
            }

            result = SyntaxFactory.ParseStatement($"{assignment.Left}?.Dispose();")
                                .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                .WithTrailingTrivia(SyntaxFactory.ElasticMarker);
            return true;
        }

        // ReSharper disable once UnusedParameter.Local
        private static bool IsAlwaysAssigned(ISymbol member, AssignmentExpressionSyntax assignment)
        {
            var symbol = member as ILocalSymbol;
            if (symbol == null)
            {
                return false;
            }

            return false;
        }

        private static ITypeSymbol MemberType(ISymbol member) =>
            (member as IFieldSymbol)?.Type ??
            (member as IPropertySymbol)?.Type ??
            (member as ILocalSymbol)?.Type ??
            (member as IParameterSymbol)?.Type;
    }
}