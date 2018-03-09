namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddNonSerializedFixProvider))]
    [Shared]
    internal class AddNonSerializedFixProvider : DocumentEditorCodeFixProvider
    {
        private static readonly AttributeTargetSpecifierSyntax FieldTargetSpecifier = SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.FieldKeyword));
        private static readonly SeparatedSyntaxList<AttributeSyntax> NonSerializedList = SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.ParseName(nameof(NonSerialized))));

        private static readonly AttributeListSyntax NonSerializedWithTargetSpecifier = SyntaxFactory.AttributeList(
            FieldTargetSpecifier,
            NonSerializedList);

        private static readonly AttributeListSyntax NonSerialized = SyntaxFactory.AttributeList(
            NonSerializedList);

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(GU0050IgnoreEventsWhenSerializing.DiagnosticId);

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
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

                var eventField = syntaxRoot.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<EventFieldDeclarationSyntax>();
                if (eventField != null)
                {
                    context.RegisterCodeFix(
                        "Add [field:NonSerialized].",
                        (editor, _) => editor.ReplaceNode(eventField, eventField.AddAttributeLists(NonSerializedWithTargetSpecifier)),
                        nameof(AddNonSerializedFixProvider),
                        diagnostic);
                    continue;
                }

                var field = syntaxRoot.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<FieldDeclarationSyntax>();
                if (field != null)
                {
                    context.RegisterCodeFix(
                        "Add [NonSerialized].",
                        (editor, _) => editor.ReplaceNode(field, field.AddAttributeLists(NonSerialized)),
                        nameof(AddNonSerializedFixProvider),
                        diagnostic);
                }
            }
        }
    }
}