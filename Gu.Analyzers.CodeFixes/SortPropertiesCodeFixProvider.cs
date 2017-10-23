namespace Gu.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
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
        public override FixAllProvider GetFixAllProvider() => BacthFixer.Default;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false) as CompilationUnitSyntax;
            if (syntaxRoot == null)
            {
                return;
            }

            foreach (var diagnostic in context.Diagnostics)
            {
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText) || token.IsMissing || syntaxRoot.Members.Count != 1)
                {
                    continue;
                }

                var property = (BasePropertyDeclarationSyntax)syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                if (!property.IsPropertyOrIndexer())
                {
                    continue;
                }

                using (var sorted = new SortedMembers())
                {
                    sorted.Sort(property);
                    var updated = new SortRewriter(sorted).Visit(syntaxRoot);
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Move property.",
                            _ => Task.FromResult(context.Document.WithSyntaxRoot(updated)),
                           this.GetType().FullName),
                        diagnostic);
                }
            }
        }

        private class SortedMembers : IDisposable
        {
            private readonly Pool<Dictionary<TypeDeclarationSyntax, List<MemberDeclarationSyntax>>>.Pooled sorted;

            public SortedMembers()
            {
                this.sorted = DictionaryPool<TypeDeclarationSyntax, List<MemberDeclarationSyntax>>.Create();
            }

            public void Sort(TypeDeclarationSyntax type)
            {
                foreach (var member in type.Members)
                {
                    var property = member as BasePropertyDeclarationSyntax;
                    if (property.IsPropertyOrIndexer())
                    {
                        this.Sort(property);
                    }
                }
            }

            public void Sort(BasePropertyDeclarationSyntax propertyDeclaration)
            {
                var type = propertyDeclaration.FirstAncestorOrSelf<TypeDeclarationSyntax>();
                if (type == null)
                {
                    return;
                }

                if (!this.sorted.Item.TryGetValue(type, out List<MemberDeclarationSyntax> members))
                {
                    members = new List<MemberDeclarationSyntax>(type.Members);
                    this.sorted.Item[type] = members;
                }

                var fromIndex = members.IndexOf(propertyDeclaration);
                members.RemoveAt(fromIndex);
                var toIndex = 0;
                for (var i = 0; i < members.Count; i++)
                {
                    var member = members[i];
                    if (member is FieldDeclarationSyntax ||
                        member is ConstructorDeclarationSyntax ||
                        member is BaseFieldDeclarationSyntax)
                    {
                        toIndex = i + 1;
                        continue;
                    }

                    if (member is MethodDeclarationSyntax)
                    {
                        toIndex = i;
                        break;
                    }

                    var otherPropertyDeclaration = member as BasePropertyDeclarationSyntax;
                    if (otherPropertyDeclaration == null || !otherPropertyDeclaration.IsPropertyOrIndexer())
                    {
                        continue;
                    }

                    if (GU0020SortProperties.PropertyPositionComparer.Default.Compare(propertyDeclaration, otherPropertyDeclaration) == 0 &&
                        fromIndex < i)
                    {
                        toIndex = i + 1;
                        continue;
                    }

                    if (GU0020SortProperties.PropertyPositionComparer.Default.Compare(propertyDeclaration, otherPropertyDeclaration) > 0)
                    {
                        toIndex = i + 1;
                        continue;
                    }

                    if (GU0020SortProperties.PropertyPositionComparer.Default.Compare(propertyDeclaration, otherPropertyDeclaration) < 0)
                    {
                        toIndex = i;
                        break;
                    }
                }

                members.Insert(toIndex, propertyDeclaration);
            }

            public void Dispose()
            {
                this.sorted?.Dispose();
            }

            public bool TryGetSorted(SyntaxNode node, out MemberDeclarationSyntax sortedNode)
            {
                sortedNode = null;
                var member = node as MemberDeclarationSyntax;
                if (member == null || member is TypeDeclarationSyntax || member is NamespaceDeclarationSyntax)
                {
                    return false;
                }

                var type = member.FirstAncestorOrSelf<TypeDeclarationSyntax>();
                if (type == null)
                {
                    return false;
                }

                if (this.sorted.Item.TryGetValue(type, out List<MemberDeclarationSyntax> sortedMembers))
                {
                    var index = type.Members.IndexOf(member);
                    if (index == -1)
                    {
                        return false;
                    }

                    sortedNode = sortedMembers[index];
                    if (index == 0)
                    {
                        if (sortedNode.HasLeadingTrivia)
                        {
                            var leadingTrivia = sortedNode.GetLeadingTrivia();
                            if (leadingTrivia.Any() &&
                                leadingTrivia[0].IsKind(SyntaxKind.EndOfLineTrivia))
                            {
                                sortedNode = sortedNode.WithLeadingTrivia(leadingTrivia.RemoveAt(0));
                            }
                        }
                    }
                    else if (!(sortedNode is FieldDeclarationSyntax))
                    {
                        if (sortedNode.HasLeadingTrivia)
                        {
                            var leadingTrivia = sortedNode.GetLeadingTrivia();
                            if (!leadingTrivia[0].IsKind(SyntaxKind.EndOfLineTrivia))
                            {
                                sortedNode = sortedNode.WithLeadingTrivia(leadingTrivia.Insert(0, SyntaxFactory.EndOfLine(Environment.NewLine)));
                            }
                        }
                        else
                        {
                            sortedNode = sortedNode.WithLeadingTrivia(SyntaxTriviaList.Create(SyntaxFactory.EndOfLine(Environment.NewLine)));
                        }
                    }

                    return true;
                }

                return true;
            }
        }

        private class SortRewriter : CSharpSyntaxRewriter
        {
            private readonly SortedMembers sorted;

            public SortRewriter(SortedMembers sorted)
            {
                this.sorted = sorted;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (node is TypeDeclarationSyntax type)
                {
                    this.sorted.Sort(type);
                    return base.Visit(node);
                }

                if (this.sorted.TryGetSorted(node, out MemberDeclarationSyntax sortedNode))
                {
                    return base.Visit(sortedNode);
                }

                return base.Visit(node);
            }
        }

        private class BacthFixer : FixAllProvider
        {
            public static readonly BacthFixer Default = new BacthFixer();
            private static readonly ImmutableArray<FixAllScope> SupportedFixAllScopes = ImmutableArray.Create(FixAllScope.Document);

            private BacthFixer()
            {
            }

            public override IEnumerable<FixAllScope> GetSupportedFixAllScopes()
            {
                return SupportedFixAllScopes;
            }

            [SuppressMessage("ReSharper", "RedundantCaseLabel", Justification = "Mute R#")]
            public override Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
            {
                switch (fixAllContext.Scope)
                {
                    case FixAllScope.Document:
                        return Task.FromResult(CodeAction.Create(
                            "Sort properties.",
                            _ => FixDocumentAsync(fixAllContext),
                            this.GetType().Name));
                    case FixAllScope.Project:
                    case FixAllScope.Solution:
                    case FixAllScope.Custom:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            private static async Task<Document> FixDocumentAsync(FixAllContext context)
            {
                var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                              .ConfigureAwait(false);
                using (var sorted = new SortedMembers())
                {
                    var updated = new SortRewriter(sorted).Visit(syntaxRoot);
                    return context.Document.WithSyntaxRoot(updated);
                }
            }
        }
    }
}