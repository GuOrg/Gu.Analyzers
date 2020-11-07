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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NonSerializedFix))]
    [Shared]
    internal class NonSerializedFix : DocumentEditorCodeFixProvider
    {
        private static readonly SeparatedSyntaxList<AttributeSyntax> NonSerializedList = SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.ParseName(nameof(NonSerialized))));

        private static readonly AttributeListSyntax NonSerializedWithTargetSpecifier = SyntaxFactory.AttributeList(
            SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.FieldKeyword)),
            NonSerializedList);

        private static readonly AttributeListSyntax NonSerialized = SyntaxFactory.AttributeList(NonSerializedList);

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(Descriptors.GU0050IgnoreEventsWhenSerializing.Id);

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot is { } &&
                    syntaxRoot.TryFindNodeOrAncestor(diagnostic, out EventFieldDeclarationSyntax? eventField))
                {
                    context.RegisterCodeFix(
                        "[field:NonSerialized].",
                        (editor, _) => editor.ReplaceNode(
                            eventField,
                            x => x.AddAttributeLists(NonSerializedWithTargetSpecifier)
                                  .WithLeadingTriviaFrom(x)),
                        nameof(NonSerializedFix),
                        diagnostic);
                    continue;
                }

                if (syntaxRoot is { } &&
                    syntaxRoot.TryFindNodeOrAncestor(diagnostic, out FieldDeclarationSyntax? field))
                {
                    context.RegisterCodeFix(
                        "[NonSerialized].",
                        (editor, _) => editor.ReplaceNode(
                            field,
                            x => x.AddAttributeLists(NonSerialized)
                                  .WithLeadingTriviaFrom(x)),
                        nameof(NonSerializedFix),
                        diagnostic);
                }
            }
        }
    }
}
