namespace Gu.Analyzers.Refactoring
{
    using System.Composition;
    using System.Threading.Tasks;

    using Gu.Roslyn.CodeFixExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeRefactorings;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(SplitStringRefactoring))]
    [Shared]
    internal class ParameterRefactoring : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            if (syntaxRoot.FindNode(context.Span) is ParameterSyntax parameter &&
                parameter is { Parent: ParameterListSyntax { Parent: ConstructorDeclarationSyntax { Parent: TypeDeclarationSyntax type } ctor } parameterList } &&
                parameterList.Parameters.Count > 1)
            {
                if (Assignment(parameter, ctor.Body.Statements) is { } assignment)
                {
                    var indexOf = parameterList.Parameters.IndexOf(parameter);
                    for (var i = indexOf - 1; i >= 0; i--)
                    {
                        if (Assignment(parameterList.Parameters[i], ctor.Body.Statements) is { } previous)
                        {
                            if (assignment.SpanStart < previous.SpanStart)
                            {
                                context.RegisterRefactoring(
                                    CodeAction.Create(
                                        "Move assignment to match parameter position.",
                                        _ => Task.FromResult(context.Document.WithSyntaxRoot(
                                                                 syntaxRoot.ReplaceNode(
                                                                     ctor.Body,
                                                                     ctor.Body.WithStatements(
                                                                         ctor.Body.Statements.Move(
                                                                             ctor.Body.Statements.IndexOf(assignment),
                                                                             ctor.Body.Statements.IndexOf(previous))))))));
                            }
                        }
                    }

                    for (var i = indexOf + 1; i < parameterList.Parameters.Count; i++)
                    {
                        if (Assignment(parameterList.Parameters[i], ctor.Body.Statements) is { } next)
                        {
                            if (assignment.SpanStart > next.SpanStart)
                            {
                                context.RegisterRefactoring(
                                    CodeAction.Create(
                                        "Move assignment to match parameter position.",
                                        _ => Task.FromResult(context.Document.WithSyntaxRoot(
                                                                 syntaxRoot.ReplaceNode(
                                                                     ctor.Body,
                                                                     ctor.Body.WithStatements(
                                                                         ctor.Body.Statements.Move(
                                                                             ctor.Body.Statements.IndexOf(assignment),
                                                                             ctor.Body.Statements.IndexOf(next))))))));
                            }
                        }
                    }
                }
            }
        }

        private static StatementSyntax? Assignment(ParameterSyntax? parameter, SyntaxList<StatementSyntax> statements)
        {
            if (parameter is null)
            {
                return null;
            }

            foreach (var statement in statements)
            {
                if (statement is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Right: IdentifierNameSyntax right } } &&
                    right.Identifier.ValueText == parameter.Identifier.ValueText)
                {
                    return statement;
                }
            }

            return null;
        }
    }
}
