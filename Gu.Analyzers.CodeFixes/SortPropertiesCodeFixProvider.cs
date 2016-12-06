namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SortPropertiesCodeFixProvider))]
    [Shared]
    internal class SortPropertiesCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(GU0020SortProperties.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false) as CompilationUnitSyntax;
            if (syntaxRoot == null)
            {
                return;
            }

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText) || token.IsMissing || syntaxRoot.Members.Count != 1)
                {
                    continue;
                }

                var property = (BasePropertyDeclarationSyntax)syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                var propertySymbol = (IPropertySymbol)semanticModel.GetDeclaredSymbol(property, context.CancellationToken);
                var type = propertySymbol.ContainingType;
                using (var properties = GU0020SortProperties.SortedProperties.Create(type))
                {
                    if (!properties.IsSorted)
                    {
                        var index = properties.IndexOfSorted(propertySymbol);
                        if (index == 0)
                        {
                            var previousSymbol = properties.Sorted[index + 1];
                            var nextProperty = (BasePropertyDeclarationSyntax)await previousSymbol.DeclaringSyntaxReferences[0].GetSyntaxAsync(context.CancellationToken)
                                              .ConfigureAwait(false);

                            var parent = (TypeDeclarationSyntax)property.Parent;
                            if (parent.Members.IndexOf(property) == parent.Members.IndexOf(nextProperty) - 1)
                            {
                                continue;
                            }

                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    "Sort properties.",
                                    _ => MoveBeforeAsync(context, syntaxRoot, nextProperty, property),
                                    nameof(SortPropertiesCodeFixProvider)),
                                diagnostic);
                        }
                        else
                        {
                            var previousSymbol = properties.Sorted[index - 1];
                            var previousProperty = (BasePropertyDeclarationSyntax)await previousSymbol.DeclaringSyntaxReferences[0].GetSyntaxAsync(context.CancellationToken)
                                              .ConfigureAwait(false);

                            var parent = (TypeDeclarationSyntax)property.Parent;
                            if (parent.Members.IndexOf(property) == parent.Members.IndexOf(previousProperty) + 1)
                            {
                                continue;
                            }

                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    "Sort properties.",
                                    _ => MoveAfterAsync(context, syntaxRoot, previousProperty, property),
                                    nameof(SortPropertiesCodeFixProvider)),
                                diagnostic);
                        }
                    }
                }
            }
        }

        private static Task<Document> MoveAfterAsync(CodeFixContext context, CompilationUnitSyntax syntaxRoot, BasePropertyDeclarationSyntax previous, BasePropertyDeclarationSyntax property)
        {
            syntaxRoot = syntaxRoot.TrackNodes(previous, property);
            var trackedProperty = syntaxRoot.GetCurrentNode(property);
            syntaxRoot = syntaxRoot.RemoveNode(trackedProperty, SyntaxRemoveOptions.KeepNoTrivia);
            var trackedPrevious = syntaxRoot.GetCurrentNode(previous);
            syntaxRoot = syntaxRoot.InsertNodesAfter(trackedPrevious, new[] { property });
            return Task.FromResult(context.Document.WithSyntaxRoot(syntaxRoot));
        }

        private static Task<Document> MoveBeforeAsync(CodeFixContext context, CompilationUnitSyntax syntaxRoot, BasePropertyDeclarationSyntax next, BasePropertyDeclarationSyntax property)
        {
            syntaxRoot = syntaxRoot.TrackNodes(next, property);
            var trackedProperty = syntaxRoot.GetCurrentNode(property);
            syntaxRoot = syntaxRoot.RemoveNode(trackedProperty, SyntaxRemoveOptions.KeepNoTrivia);
            var trackedNext = syntaxRoot.GetCurrentNode(next);
            syntaxRoot = syntaxRoot.InsertNodesBefore(trackedNext, new[] { property });
            return Task.FromResult(context.Document.WithSyntaxRoot(syntaxRoot));
        }
    }
}