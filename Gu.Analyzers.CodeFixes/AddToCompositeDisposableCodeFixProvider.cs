namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddToCompositeDisposableCodeFixProvider))]
    [Shared]
    internal class AddToCompositeDisposableCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(GU0033DontIgnoreReturnValueOfTypeIDisposable.DiagnosticId);

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
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
                if (diagnostic.Id == GU0033DontIgnoreReturnValueOfTypeIDisposable.DiagnosticId)
                {
                    var statement = node.FirstAncestorOrSelf<ExpressionStatementSyntax>();
                    if (statement != null)
                    {
                        if (TryGetField(statement, semanticModel, context.CancellationToken, out IFieldSymbol field))
                        {
                            var usesUnderscoreNames = statement.UsesUnderscoreNames(semanticModel, context.CancellationToken);
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    "Add to CompositeDisposable.",
                                    _ => ApplyAddToCompositeDisposableFixAsync(context, statement, field, usesUnderscoreNames),
                                    nameof(AddToCompositeDisposableCodeFixProvider)),
                                diagnostic);
                        }
                    }
                }
            }
        }

        private static async Task<Document> ApplyAddToCompositeDisposableFixAsync(CodeFixContext context, ExpressionStatementSyntax statement, IFieldSymbol field, bool usesUnderscoreNames)
        {
            var editor = await DocumentEditor.CreateAsync(context.Document)
                                             .ConfigureAwait(false);
            var memberAccessExpressionSyntax = usesUnderscoreNames
                                                   ? (MemberAccessExpressionSyntax)editor
                                                       .Generator.MemberAccessExpression(
                                                           SyntaxFactory.IdentifierName(field.Name),
                                                           "Add")
                                                   : (MemberAccessExpressionSyntax)editor
                                                       .Generator.MemberAccessExpression(
                                                           editor.Generator.MemberAccessExpression(
                                                               SyntaxFactory.ThisExpression(),
                                                               SyntaxFactory.IdentifierName(field.Name)),
                                                           "Add");

            editor.ReplaceNode(
                statement,
                SyntaxFactory.ExpressionStatement(
                    (InvocationExpressionSyntax)editor.Generator.InvocationExpression(
                        memberAccessExpressionSyntax,
                        statement.Expression)));
            return editor.GetChangedDocument();
        }

        private static bool TryGetField(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken, out IFieldSymbol field)
        {
            field = null;
            var typeDeclaration = node.FirstAncestor<TypeDeclarationSyntax>();
            if (typeDeclaration == null)
            {
                return false;
            }

            var type = semanticModel.GetDeclaredSymbolSafe(typeDeclaration, cancellationToken);
            if (type == null)
            {
                return false;
            }

            foreach (var member in type.GetMembers())
            {
                if (member is IFieldSymbol candidateField &&
                    candidateField.Type == KnownSymbol.CompositeDisposable)
                {
                    field = candidateField;
                    return true;
                }
            }

            return false;
        }
    }
}