namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullCheckParameterCodeFixProvider))]
    [Shared]
    internal class NullCheckParameterCodeFixProvider : DocumentEditorCodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            GU0012NullCheckParameter.DiagnosticId);

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                if (node is IdentifierNameSyntax identifierName)
                {
                    context.RegisterCodeFix(
                        "Throw if null.",
                        (editor, _) => editor.ReplaceNode(
                            node,
                            SyntaxFactory.ParseExpression($"{identifierName.Identifier.ValueText} ?? throw new System.ArgumentNullException(nameof({identifierName.Identifier.ValueText}))").WithSimplifiedNames()),
                        this.GetType(),
                        diagnostic);
                }
                else if (node is ParameterSyntax parameterSyntax)
                {
                    var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                                     .ConfigureAwait(false);
                    var method = parameterSyntax.FirstAncestor<BaseMethodDeclarationSyntax>();
                    if (method != null)
                    {
                        using (var walker = AssignmentWalker.Create(method, Search.TopLevel, semanticModel, context.CancellationToken))
                        {
                            foreach (var assinment in walker.Assignments)
                            {
                                if (assinment.Right is IdentifierNameSyntax assignedValue &&
                                    assignedValue.Identifier.ValueText == parameterSyntax.Identifier.ValueText)
                                {
                                    context.RegisterCodeFix(
                                        "Throw if null on first assignment..",
                                        (editor, _) => editor.ReplaceNode(
                                            assignedValue,
                                            SyntaxFactory.ParseExpression($"{assignedValue.Identifier.ValueText} ?? throw new System.ArgumentNullException(nameof({assignedValue.Identifier.ValueText}))").WithSimplifiedNames()),
                                        this.GetType(),
                                        diagnostic);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}