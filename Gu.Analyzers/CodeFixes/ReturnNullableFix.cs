namespace Gu.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ReturnNullableFix))]
    [Shared]
    internal class ReturnNullableFix : DocumentEditorCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            Descriptors.GU0075PreferReturnNullable.Id);

        protected override DocumentEditorFixAllProvider? FixAllProvider() => null;

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out ParameterSyntax? parameter) &&
                    parameter is { Parent: ParameterListSyntax { Parent: { } method } })
                {
                    using var walker = ReturnValueWalker.Borrow(method);
                    if (walker.All(x => x.IsEither(SyntaxKind.TrueLiteralExpression, SyntaxKind.FalseLiteralExpression)))
                    {
                        context.RegisterCodeFix(
                            "Return nullable",
                            (editor, _) => editor.ReplaceNode(
                                method,
                                x => Rewriter.Update(parameter, x)),
                            "Return nullable",
                            diagnostic);
                    }
                }
            }
        }

        private class Rewriter : CSharpSyntaxRewriter
        {
            private readonly ParameterSyntax parameter;

            private Rewriter(ParameterSyntax parameter)
            {
                this.parameter = parameter;
            }

            public override SyntaxNode VisitParameterList(ParameterListSyntax node)
            {
                var match = node.Parameters.SingleOrDefault(x => x.IsEquivalentTo(this.parameter));
                if (match is { })
                {
                    return node.RemoveNode(match, SyntaxRemoveOptions.AddElasticMarker);
                }

                return node;
            }

            public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node)
            {
                return null;
            }

            public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
            {
                if (node is { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax left } assignment } &&
                    left.Identifier.ValueText == this.parameter.Identifier.ValueText)
                {
                    return SyntaxFactory.ReturnStatement(assignment.Right);
                }

                return base.VisitExpressionStatement(node);
            }

            internal static SyntaxNode Update(ParameterSyntax parameter, SyntaxNode method)
            {
                return method switch
                {
                    MethodDeclarationSyntax m => UpdateCore(m).WithReturnType(parameter.Type.WithLeadingElasticSpace()),
                    LocalFunctionStatementSyntax m => UpdateCore(m).WithReturnType(parameter.Type.WithLeadingElasticSpace()),
                    _ => throw new NotSupportedException($"Not handling {method.Kind()} yet."),
                };

                T UpdateCore<T>(T node)
                    where T : SyntaxNode
                {
                    return (T)new Rewriter(parameter).Visit(node);
                }
            }
        }
    }
}
