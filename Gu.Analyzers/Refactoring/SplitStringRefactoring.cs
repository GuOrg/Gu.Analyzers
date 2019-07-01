namespace Gu.Analyzers.Refactoring
{
    using System;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeRefactorings;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class SplitStringRefactoring : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            if (syntaxRoot.FindNode(context.Span) is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression) &&
                literal.Token.Text.IndexOf("\\n", StringComparison.Ordinal) < literal.Token.Text.Length - 3)
            {
                context.RegisterRefactoring(
                    CodeAction.Create(
                        "Split string at newlines.",
                        _ => Task.FromResult(context.Document.WithSyntaxRoot(
                                                 syntaxRoot.ReplaceNode(
                                                     literal,
                                                     Split()))),
                        nameof(SplitStringRefactoring)));

                BinaryExpressionSyntax Split()
                {
                    var leadingWhiteSpace = SyntaxFactory.TriviaList(
                        SyntaxFactory.Whitespace(new string(' ', literal.FileLinePositionSpan(context.CancellationToken).StartLinePosition.Character)));
                    var index = literal.Token.Text.IndexOf("\\n", StringComparison.Ordinal);
                    return SyntaxFactory.BinaryExpression(
                        SyntaxKind.AddExpression,
                        StringLiteral(literal.GetLeadingTrivia(), literal.Token.Text.Substring(1, index + 1)),
                        SyntaxFactory.Token(
                            SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" ")),
                            SyntaxKind.PlusToken,
                            SyntaxFactory.TriviaList(SyntaxFactory.LineFeed)),
                        StringLiteral(leadingWhiteSpace, literal.Token.Text.Substring(index + 2, literal.Token.Text.Length - index - 3)));

                    LiteralExpressionSyntax StringLiteral(SyntaxTriviaList leading, string text)
                    {
                        return SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(
                                leading,
                                $"\"{text}\"",
                                text,
                                default));
                    }
                }
            }
        }
    }
}
