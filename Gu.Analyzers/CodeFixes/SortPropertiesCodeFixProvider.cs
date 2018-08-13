namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.AnalyzerExtensions.StyleCopComparers;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SortPropertiesCodeFixProvider))]
    [Shared]
    internal class SortPropertiesCodeFixProvider : DocumentEditorCodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(GU0020SortProperties.DiagnosticId);

        /// <inheritdoc/>
        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNodeOrAncestor<BasePropertyDeclarationSyntax>(diagnostic, out var property))
                {
                    context.RegisterCodeFix(
                        "Sort property.",
                        (editor, _) => Move(editor, property),
                        "Sort property.",
                        diagnostic);
                }
            }
        }

        private static void Move(DocumentEditor editor, BasePropertyDeclarationSyntax property)
        {
            editor.TrackNode(property);
            editor.ReplaceNode(
                (TypeDeclarationSyntax)property.Parent,
                WithMoved);

            SyntaxNode WithMoved(TypeDeclarationSyntax old)
            {
                switch (old)
                {
                    case ClassDeclarationSyntax classDeclaration:
                        return classDeclaration.WithMembers(SortPropertiesCodeFixProvider.WithMoved(old.Members, old.GetCurrentNode(property)));
                    case StructDeclarationSyntax structDeclaration:
                        return structDeclaration.WithMembers(SortPropertiesCodeFixProvider.WithMoved(old.Members, old.GetCurrentNode(property)));
                    default:
                        return old;
                }
            }
        }

        private static SyntaxList<MemberDeclarationSyntax> WithMoved(SyntaxList<MemberDeclarationSyntax> members, MemberDeclarationSyntax member)
        {
            members = members.Remove(member);
            for (var i = 0; i < members.Count; i++)
            {
                var current = members[i];
                if (MemberDeclarationComparer.Compare(member, current) < 0)
                {
                    return RemoveLeadingEndOfLine(members.Insert(i, UpdateLineFeed(member)));
                }
            }

            return RemoveLeadingEndOfLine(members.Add(UpdateLineFeed(member)));
        }

        private static SyntaxList<MemberDeclarationSyntax> RemoveLeadingEndOfLine(SyntaxList<MemberDeclarationSyntax> members)
        {
            if (members.TryFirst(out var first) &&
                first.HasLeadingTrivia &&
                first.GetLeadingTrivia() is SyntaxTriviaList triviaList &&
                triviaList.TryFirst(x => x.IsKind(SyntaxKind.EndOfLineTrivia), out var eol))
            {
                return members.Replace(
                    first,
                    first.WithLeadingTrivia(RemoveLeading()));
            }

            return members;

            SyntaxTriviaList RemoveLeading()
            {
                for (var i = triviaList.IndexOf(eol); i >= 0; i--)
                {
                    if (triviaList[i] is SyntaxTrivia trivia &&
                        (trivia.IsKind(SyntaxKind.EndOfLineTrivia) || trivia.IsKind(SyntaxKind.WhitespaceTrivia)))
                    {
                        triviaList = triviaList.Remove(trivia);
                    }
                }

                return triviaList;
            }
        }

        private static T UpdateLineFeed<T>(T member)
            where T : MemberDeclarationSyntax
        {
            if (member.HasLeadingTrivia &&
                member.GetLeadingTrivia() is SyntaxTriviaList triviaList &&
                triviaList.First() is SyntaxTrivia trivia &&
                trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                return member.WithLeadingTrivia(triviaList.Replace(trivia, SyntaxFactory.ElasticLineFeed));
            }

            return member.WithLeadingElasticLineFeed();
        }
    }
}
