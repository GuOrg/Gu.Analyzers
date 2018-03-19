namespace Gu.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;
    using Microsoft.CodeAnalysis.Rename;
    using Microsoft.CodeAnalysis.Text;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeInternalFixProvider))]
    [Shared]
    internal class MakeInternalFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(GU0072AllTypesShouldBeInternal.DiagnosticId);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
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

                var node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                if (node.FirstAncestorOrSelf<TypeDeclarationSyntax>() is TypeDeclarationSyntax typeDeclaration)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Make internal.",
                        _ => MakeInternalAsync(node, context.Document, typeDeclaration),
                        nameof(MakeInternalFixProvider)),
                        diagnostic);
                }
            }
        }

        private static Task<Document> MakeInternalAsync(SyntaxNode root, Document document, TypeDeclarationSyntax typeDeclaration)
        {
            foreach (var modifier in typeDeclaration.Modifiers)
            {
                if (modifier.Kind() == SyntaxKind.PublicKeyword)
                {
                    var syntaxToken = SyntaxFactory.Token(SyntaxKind.InternalKeyword);
                    return Task.FromResult(document.WithSyntaxRoot(root.ReplaceToken(modifier, syntaxToken)));
                }
            }

            return null;
        }
    }
}
