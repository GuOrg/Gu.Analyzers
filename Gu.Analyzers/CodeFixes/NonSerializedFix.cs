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

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(GU0050IgnoreEventsWhenSerializing.Descriptor.Id);

        /// <inheritdoc/>
        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText) ||
                    token.IsMissing)
                {
                    continue;
                }

                if (syntaxRoot.TryFindNodeOrAncestor<EventFieldDeclarationSyntax>(diagnostic, out var eventField))
                {
                    context.RegisterCodeFix(
                        "Add [field:NonSerialized].",
                        (editor, _) => editor.ReplaceNode(
                            eventField,
                            x => x.AddAttributeLists(NonSerializedWithTargetSpecifier)
                                  .WithLeadingTriviaFrom(x)),
                        nameof(NonSerializedFix),
                        diagnostic);
                    continue;
                }

                if (syntaxRoot.TryFindNodeOrAncestor<FieldDeclarationSyntax>(diagnostic, out var field))
                {
                    context.RegisterCodeFix(
                        "Add [NonSerialized].",
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
