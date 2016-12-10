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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InjectCodeFixProvider))]
    [Shared]
    internal class InjectCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(GU0007PreferInjecting.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText) || token.IsMissing)
                {
                    continue;
                }

                var node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                var objectCreation = node as ObjectCreationExpressionSyntax;
                if (objectCreation == null)
                {
                    continue;
                }

                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Inject",
                        cancellationToken => ApplyFixAsync(context, syntaxRoot, objectCreation),
                        nameof(InjectCodeFixProvider)),
                    diagnostic);
            }
        }

        private static Task<Document> ApplyFixAsync(CodeFixContext context, SyntaxNode syntaxRoot, ObjectCreationExpressionSyntax objectCreation)
        {
            var ctor = objectCreation.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
            var parameterSyntax = SyntaxFactory.Parameter(SyntaxFactory.Identifier(Parametername(objectCreation)))
                                               .WithType(objectCreation.Type);
            var updated = ctor.ReplaceNode(objectCreation, SyntaxFactory.IdentifierName(parameterSyntax.Identifier));
            updated = updated.WithParameterList(ctor.ParameterList.AddParameters(parameterSyntax));
            return Task.FromResult(context.Document.WithSyntaxRoot(syntaxRoot.ReplaceNode(ctor, updated)));
        }

        private static string Parametername(ObjectCreationExpressionSyntax objectCreation)
        {
            var type = (IdentifierNameSyntax)objectCreation.Type;
            var typeName = type.Identifier.ValueText;
            if (char.IsUpper(typeName[0]))
            {
                return new string(char.ToLower(typeName[0]), 1) + typeName.Substring(1);
            }

            return typeName;
        }
    }
}