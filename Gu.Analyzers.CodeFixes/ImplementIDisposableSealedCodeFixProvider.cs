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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ImplementIDisposableSealedCodeFixProvider))]
    [Shared]
    internal class ImplementIDisposableSealedCodeFixProvider : CodeFixProvider
    {
        ////private static readonly SyntaxTriviaList ElasticTriviaList = SyntaxTriviaList.Create(SyntaxFactory.ElasticMarker);

        private static readonly MethodDeclarationSyntax EmptyDisposeMethod = SyntaxFactory.MethodDeclaration(
            attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
            modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
            returnType: SyntaxFactory.ParseName("void"),
            explicitInterfaceSpecifier: null,
            identifier: SyntaxFactory.Identifier("Dispose"),
            typeParameterList: null,
            parameterList: SyntaxFactory.ParameterList(),
            constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
            body: SyntaxFactory.Block(),
            semicolonToken: SyntaxFactory.Token(SyntaxKind.None));

        // ReSharper disable once InconsistentNaming
        private static readonly SimpleBaseTypeSyntax IDisposableInterface = SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("IDisposable"));

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
            var classDeclaration = typeDeclaration as ClassDeclarationSyntax;
            if (classDeclaration != null)
            {
                var updated = classDeclaration.AddMembers(EmptyDisposeMethod);
                if (!Disposable.IsAssignableTo(type))
                {
                    updated = updated.AddBaseListTypes(IDisposableInterface);
                }

                if (!IsSealed(typeDeclaration))
                {
                    updated = updated.AddModifiers(SyntaxFactory.Token(SyntaxKind.SealedKeyword));
                }

                return Task.FromResult(context.Document.WithSyntaxRoot(syntaxRoot.ReplaceNode(typeDeclaration, updated)));
            }

            var structDeclaration = typeDeclaration as StructDeclarationSyntax;
            if (structDeclaration != null)
            {
                var updated = structDeclaration.AddMembers(EmptyDisposeMethod);
                if (!Disposable.IsAssignableTo(type))
                {
                    updated = updated.AddBaseListTypes(IDisposableInterface);
                }

                if (!IsSealed(typeDeclaration))
                {
                    updated = updated.AddModifiers(SyntaxFactory.Token(SyntaxKind.SealedKeyword));
                }

                return Task.FromResult(context.Document.WithSyntaxRoot(syntaxRoot.ReplaceNode(typeDeclaration, updated)));
            }

            return Task.FromResult(context.Document);
        }

        private static bool IsSealed(TypeDeclarationSyntax type)
        {
            return type.Modifiers.Any(SyntaxKind.SealedKeyword);
        }
    }
}