namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Formatting;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullCheckParameterCodeFixProvider))]
    [Shared]
    internal class NullCheckParameterCodeFixProvider : DocumentEditorCodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            GU0012NullCheckParameter.DiagnosticId);

        /// <inheritdoc/>
        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
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
                else if (node is ParameterSyntax parameter)
                {
                    var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                                     .ConfigureAwait(false);
                    var method = parameter.FirstAncestor<BaseMethodDeclarationSyntax>();
                    if (method != null)
                    {
                        using (var walker = AssignmentExecutionWalker.Borrow(method, Search.TopLevel, semanticModel, context.CancellationToken))
                        {
                            if (TryFirstAssignedWith(parameter, walker.Assignments, out var assignedValue))
                            {
                                context.RegisterCodeFix(
                                    "Throw if null on first assignment.",
                                    (editor, _) => editor.ReplaceNode(
                                        assignedValue,
                                        SyntaxFactory.ParseExpression($"{assignedValue.Identifier.ValueText} ?? throw new System.ArgumentNullException(nameof({assignedValue.Identifier.ValueText}))").WithSimplifiedNames()),
                                    this.GetType(),
                                    diagnostic);
                            }
                            else if (method.Body != null)
                            {
                                context.RegisterCodeFix(
                                    "Add null check.",
                                    (editor, _) => editor.ReplaceNode(
                                        method.Body,
                                        method.Body.InsertNodesBefore(
                                            method.Body.Statements[0],
                                            new[] { IfNullThrow(parameter.Identifier.ValueText) })),
                                    this.GetType(),
                                    diagnostic);
                            }
                        }
                    }
                }
            }
        }

        private static bool TryFirstAssignedWith(ParameterSyntax parameter, IReadOnlyList<AssignmentExpressionSyntax> assignments, out IdentifierNameSyntax assignedValue)
        {
            foreach (var assignment in assignments)
            {
                if (assignment.Right is IdentifierNameSyntax candidate &&
                    candidate.Identifier.ValueText == parameter.Identifier.ValueText)
                {
                    assignedValue = candidate;
                    return true;
                }
            }

            assignedValue = null;
            return false;
        }

        private static StatementSyntax IfNullThrow(string name)
        {
            var code = StringBuilderPool.Borrow()
                                        .AppendLine($"if ({name} == null)")
                                        .AppendLine("{")
                                        .AppendLine($"    throw new System.ArgumentNullException(nameof({name}));")
                                        .AppendLine("}")
                                        .Return();
            return SyntaxFactory.ParseStatement(code)
                                .WithSimplifiedNames()
                                .WithAdditionalAnnotations(Formatter.Annotation)
                                .WithTrailingElasticLineFeed();
        }
    }
}
