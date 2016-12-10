namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ImplementIDisposableSealedCodeFixProvider))]
    [Shared]
    internal class ImplementIDisposableSealedCodeFixProvider : CodeFixProvider
    {
        // ReSharper disable once InconsistentNaming
        private static readonly TypeSyntax IDisposableInterface = SyntaxFactory.ParseTypeName("IDisposable");

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            GU0031DisposeMember.DiagnosticId,
            "CS0535");

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
                if (diagnostic.Id == "CS0535" &&
                    !diagnostic.GetMessage(CultureInfo.InvariantCulture)
                               .EndsWith("does not implement interface member 'IDisposable.Dispose()'"))
                {
                    continue;
                }

                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText) ||
                    token.IsMissing)
                {
                    continue;
                }

                var typeDeclaration = syntaxRoot.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<TypeDeclarationSyntax>();
                if (typeDeclaration == null)
                {
                    continue;
                }

                if (diagnostic.Id == GU0031DisposeMember.DiagnosticId && Disposable.IsAssignableTo(semanticModel.GetDeclaredSymbol(typeDeclaration)))
                {
                    continue;
                }

                context.RegisterCodeFix(
                CodeAction.Create(
                    "Implement IDisposable and make class sealed.",
                    cancellationToken =>
                        ApplyImplementIDisposableSealedFixAsync(
                            context,
                            semanticModel,
                            cancellationToken,
                            syntaxRoot,
                            typeDeclaration),
                    nameof(ImplementIDisposableSealedCodeFixProvider)),
                diagnostic);
            }
        }

        private static Task<Document> ApplyImplementIDisposableSealedFixAsync(CodeFixContext context, SemanticModel semanticModel, CancellationToken cancellationToken, SyntaxNode syntaxRoot, TypeDeclarationSyntax typeDeclaration)
        {
            var type = (ITypeSymbol)semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);
            var syntaxGenerator = SyntaxGenerator.GetGenerator(context.Document);

            var updated = syntaxGenerator.AddMembers(
                typeDeclaration,
                syntaxGenerator.MethodDeclaration("Dispose", accessibility: Accessibility.Public));
            if (!Disposable.IsAssignableTo(type))
            {
                updated = syntaxGenerator.AddBaseType(updated, IDisposableInterface);
            }

            if (!IsSealed(typeDeclaration))
            {
                updated = syntaxGenerator.WithModifiers(updated, DeclarationModifiers.Sealed);
            }

            return Task.FromResult(context.Document.WithSyntaxRoot(syntaxRoot.ReplaceNode(typeDeclaration, updated)));
        }

        private static bool IsSealed(TypeDeclarationSyntax type)
        {
            return type.Modifiers.Any(SyntaxKind.SealedKeyword);
        }
    }
}