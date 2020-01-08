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
                    parameter is { Parent: ParameterListSyntax { Parent: { } methodOrLocalFunction } })
                {
                    if (CanRewrite())
                    {
                        context.RegisterCodeFix(
                            "Return nullable",
                            (editor, _) => editor.ReplaceNode(
                                methodOrLocalFunction,
                                x => Rewriter.Update(parameter, x)),
                            "Return nullable",
                            diagnostic);
                    }
                    else
                    {
                        context.RegisterCodeFix(
                            "UNSAFE Return nullable",
                            (editor, _) => editor.ReplaceNode(
                                methodOrLocalFunction,
                                x => Rewriter.Update(parameter, x)),
                            "UNSAFE Return nullable",
                            diagnostic);
                    }

                    bool CanRewrite()
                    {
                        using var walker = ReturnValueWalker.Borrow(methodOrLocalFunction);
                        foreach (var value in walker.ReturnValues)
                        {
                            switch (value)
                            {
                                case LiteralExpressionSyntax { Token: { ValueText: "true" }, Parent: ReturnStatementSyntax statement }
                                    when IsPreviousStatementAssigning(statement, parameter):
                                case LiteralExpressionSyntax { Token: { ValueText: "false" }, Parent: ReturnStatementSyntax _ }:
                                case BinaryExpressionSyntax { Left: IdentifierNameSyntax left, OperatorToken: { ValueText: "!=" }, Right: LiteralExpressionSyntax { Token: { ValueText: "null" } }, Parent: ReturnStatementSyntax _ }
                                    when left.Identifier.ValueText == parameter.Identifier.ValueText:
                                    break;
                                default:
                                    return false;
                            }
                        }

                        return true;
                    }
                }
            }
        }

        private static bool IsPreviousStatementAssigning(StatementSyntax statement, ParameterSyntax parameter)
        {
            return statement.Parent is BlockSyntax { Statements: { } statements } &&
                   statements.TryElementAt(statements.IndexOf(statement) - 1, out var previous) &&
                   previous is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax left } } &&
                   left.Identifier.ValueText == parameter.Identifier.ValueText;
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
                return node.RemoveNode(
                    node.Parameters.Single(x => x.IsEquivalentTo(this.parameter)),
                    SyntaxRemoveOptions.AddElasticMarker);
            }

            public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node)
            {
                return node switch
                {
                    { Expression: LiteralExpressionSyntax { Token: { ValueText: "true" } } } => null,
                    { Expression: LiteralExpressionSyntax { Token: { ValueText: "false" } } } => null,
                    { Expression: BinaryExpressionSyntax { Left: IdentifierNameSyntax left, OperatorToken: { ValueText: "!=" }, Right: LiteralExpressionSyntax { Token: { ValueText: "null" } }, Parent: ReturnStatementSyntax _ } }
                        when left.Identifier.ValueText == this.parameter.Identifier.ValueText
                        => node.WithExpression(left.WithoutTrailingTrivia()),
                    _ => node.WithExpression(
                        SyntaxFactory.ConditionalExpression(
                            node.Expression,
                            SyntaxFactory.IdentifierName(this.parameter.Identifier),
                            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))),
                };
            }

            public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
            {
                if (node is { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax left } assignment } &&
                    left.Identifier.ValueText == this.parameter.Identifier.ValueText)
                {
                    if (node.Parent is BlockSyntax { Statements: { } statements, Parent: { } parent } &&
                        statements[0] == node &&
                        parent.IsEither(SyntaxKind.MethodDeclaration, SyntaxKind.LocalFunctionStatement))
                    {
                        return null;
                    }

                    return SyntaxFactory.ReturnStatement(assignment.Right);
                }

                return base.VisitExpressionStatement(node);
            }

            public override SyntaxNode VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
            {
                if (node is { ParameterList: { Parameters: { } parameters } } &&
                    parameters.Any(x => x.IsEquivalentTo(this.parameter)))
                {
                    return base.VisitLocalFunctionStatement(node);
                }

                return node;
            }

            internal static SyntaxNode Update(ParameterSyntax parameter, SyntaxNode method)
            {
                return method switch
                {
                    MethodDeclarationSyntax m => UpdateCore(m).WithReturnType(ReturnType().WithTriviaFrom(m.ReturnType)),
                    LocalFunctionStatementSyntax m => UpdateCore(m).WithReturnType(ReturnType().WithTriviaFrom(m.ReturnType)),
                    _ => throw new NotSupportedException($"Not handling {method.Kind()} yet."),
                };

                T UpdateCore<T>(T node)
                    where T : SyntaxNode
                {
                    return (T)new Rewriter(parameter).Visit(node);
                }

                TypeSyntax ReturnType()
                {
                    return parameter.Type switch
                    {
                        NullableTypeSyntax nullable => nullable,
                        { } type => SyntaxFactory.NullableType(type),
                        _ => throw new NotSupportedException($"Missing type."),
                    };
                }
            }
        }
    }
}
