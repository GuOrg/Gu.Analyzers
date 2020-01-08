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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseParameterFix))]
    [Shared]
    internal class UseParameterFix : DocumentEditorCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(Descriptors.GU0014PreferParameter.Id);

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (diagnostic.Properties.TryGetValue("Name", out var name))
                {
                    if (syntaxRoot.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is MemberAccessExpressionSyntax memberAccess)
                    {
                        context.RegisterCodeFix(
                            "Prefer parameter.",
                            (editor, _) => editor.ReplaceNode(
                                memberAccess,
                                SyntaxFactory.IdentifierName(name)
                                             .WithLeadingTriviaFrom(memberAccess)),
                            "Prefer parameter.",
                            diagnostic);
                    }
                    else if (syntaxRoot.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is IdentifierNameSyntax identifierName)
                    {
                        context.RegisterCodeFix(
                            "Prefer parameter.",
                            (editor, _) => editor.ReplaceNode(
                                identifierName,
                                identifierName.WithIdentifier(SyntaxFactory.Identifier(name))
                                              .WithLeadingTriviaFrom(identifierName)),
                            "Prefer parameter.",
                            diagnostic);
                    }
                }
            }
        }
    }
}
