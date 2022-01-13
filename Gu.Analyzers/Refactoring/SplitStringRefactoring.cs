namespace Gu.Analyzers.Refactoring;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Gu.Roslyn.AnalyzerExtensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(SplitStringRefactoring))]
[Shared]
internal class SplitStringRefactoring : CodeRefactoringProvider
{
    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                      .ConfigureAwait(false);
        if (syntaxRoot?.FindNode(context.Span) is LiteralExpressionSyntax literal &&
            literal.IsKind(SyntaxKind.StringLiteralExpression) &&
            !literal.Token.IsVerbatimStringLiteral() &&
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
                var lines = Lines()
                    .ToImmutableArray();

                var binaryExpression = SyntaxFactory.BinaryExpression(
                    SyntaxKind.AddExpression,
                    StringLiteral(literal.GetLeadingTrivia(), lines[0]),
                    SyntaxFactory.Token(
                        SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" ")),
                        SyntaxKind.PlusToken,
                        SyntaxFactory.TriviaList(SyntaxFactory.LineFeed)),
                    StringLiteral(leadingWhiteSpace, lines[1]));
                for (var i = 2; i < lines.Length; i++)
                {
                    binaryExpression = SyntaxFactory.BinaryExpression(
                        SyntaxKind.AddExpression,
                        binaryExpression,
                        SyntaxFactory.Token(
                            SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" ")),
                            SyntaxKind.PlusToken,
                            SyntaxFactory.TriviaList(SyntaxFactory.LineFeed)),
                        StringLiteral(leadingWhiteSpace, lines[i]));
                }

                return binaryExpression;

                IEnumerable<string> Lines()
                {
                    var start = 1;
                    for (var i = 1; i < literal.Token.Text.Length - 2; i++)
                    {
                        if (literal.Token.Text[i - 1] == '\\' &&
                            literal.Token.Text[i] == 'n')
                        {
                            yield return literal.Token.Text.Slice(start, i + 1);
                            start = i + 1;
                        }
                    }

                    yield return literal.Token.Text.Slice(start, literal.Token.Text.Length - 1);
                }

                static LiteralExpressionSyntax StringLiteral(SyntaxTriviaList leading, string text)
                {
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(
                            leading: leading,
                            text: $"\"{text}\"",
                            value: text,
                            trailing: SyntaxFactory.TriviaList()));
                }
            }
        }
    }
}
