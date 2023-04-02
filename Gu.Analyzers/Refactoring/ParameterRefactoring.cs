namespace Gu.Analyzers.Refactoring;

using System;
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

        if (syntaxRoot?.FindNode(context.Span).FirstAncestorOrSelf<ParameterSyntax>() is { } parameter &&
            parameter is { Parent: ParameterListSyntax { Parent: ConstructorDeclarationSyntax { Parent: TypeDeclarationSyntax _ } ctor } parameterList } &&
            parameterList.Parameters.Count > 1)
        {
            if (ctor.Body is { } &&
                ShouldMoveAssignment(parameter, ctor) is { } moveAssignment)
            {
                context.RegisterRefactoring(
                    CodeAction.Create(
                        "Move assignment to match parameter position.",
                        _ => Task.FromResult(
                            context.Document.WithSyntaxRoot(
                                syntaxRoot.ReplaceNode(
                                    ctor.Body,
                                    ctor.Body.WithStatements(
                                        ctor.Body.Statements.Move(
                                            ctor.Body.Statements.IndexOf(moveAssignment.From),
                                            ctor.Body.Statements.IndexOf(moveAssignment.To)))))),
                        "Move assignment to match parameter position."));
            }

            if (ShouldMoveParameter(parameter, ctor) is { } moveParameter)
            {
                context.RegisterRefactoring(
                    CodeAction.Create(
                        "Move parameter to match assigned member position.",
                        _ => Task.FromResult(
                            context.Document.WithSyntaxRoot(
                                syntaxRoot.ReplaceNode(
                                    ctor.ParameterList,
                                    ctor.ParameterList.WithParameters(
                                        Move(
                                            ctor.ParameterList.Parameters,
                                            ctor.ParameterList.Parameters.IndexOf(moveParameter.From),
                                            ctor.ParameterList.Parameters.IndexOf(moveParameter.To)))))),
                        "Move parameter to match assigned member position."));
            }

            static SeparatedSyntaxList<T> Move<T>(SeparatedSyntaxList<T> list, int oldIndex, int newIndex)
                where T : SyntaxNode
            {
                var item = list[oldIndex];
                return list.RemoveAt(oldIndex)
                           .Insert(Math.Min(newIndex, list.Count - 1), item);
            }
        }
    }

    private static Move<StatementSyntax>? ShouldMoveAssignment(ParameterSyntax parameter, ConstructorDeclarationSyntax ctor)
    {
        if (ctor is { ParameterList: { } parameterList, Body.Statements: { } statements } &&
            Assignment(parameter, statements) is { } assignment)
        {
            var indexOf = parameterList.Parameters.IndexOf(parameter);
            for (var i = indexOf - 1; i >= 0; i--)
            {
                if (Assignment(parameterList.Parameters[i], ctor.Body.Statements) is { } previous)
                {
                    if (assignment.SpanStart < previous.SpanStart)
                    {
                        return new Move<StatementSyntax>(assignment, previous);
                    }
                }
            }

            for (var i = indexOf + 1; i < parameterList.Parameters.Count; i++)
            {
                if (Assignment(parameterList.Parameters[i], ctor.Body.Statements) is { } next)
                {
                    if (assignment.SpanStart > next.SpanStart)
                    {
                        return new Move<StatementSyntax>(assignment, next);
                    }
                }
            }
        }

        return null;
    }

    private static Move<ParameterSyntax>? ShouldMoveParameter(ParameterSyntax parameter, ConstructorDeclarationSyntax ctor)
    {
        var parameterList = ctor.ParameterList;
        if (Member(parameter, ctor) is { } member)
        {
            var indexOf = parameterList.Parameters.IndexOf(parameter);
            for (var i = indexOf - 1; i >= 0; i--)
            {
                if (Member(parameterList.Parameters[i], ctor) is { } previous)
                {
                    if (member.SpanStart < previous.SpanStart)
                    {
                        return new Move<ParameterSyntax>(parameter, parameterList.Parameters[i]);
                    }
                }
            }

            for (var i = indexOf + 1; i < parameterList.Parameters.Count; i++)
            {
                if (Member(parameterList.Parameters[i], ctor) is { } next)
                {
                    if (member.SpanStart > next.SpanStart)
                    {
                        return new Move<ParameterSyntax>(parameter, parameterList.Parameters[i]);
                    }
                }
            }
        }

        return null;

        static MemberDeclarationSyntax? Member(ParameterSyntax parameter, ConstructorDeclarationSyntax ctor)
        {
            if (ctor is { Body.Statements: { } statements } &&
                Assignment(parameter, statements) is { Expression: AssignmentExpressionSyntax assignment } &&
                Name(assignment.Left) is { } name &&
                ctor.Parent is TypeDeclarationSyntax type)
            {
                foreach (var candidate in type.Members)
                {
                    switch (candidate)
                    {
                        case FieldDeclarationSyntax { Declaration.Variables: { Count: 1 } variables }
                            when variables[0].Identifier.ValueText == name:
                            return candidate;
                        case PropertyDeclarationSyntax { Identifier: { } identifier }
                            when identifier.ValueText == name:
                            return candidate;
                    }
                }
            }

            return null;

            static string? Name(ExpressionSyntax left)
            {
                return left switch
                {
                    IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
                    MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax _, Name: IdentifierNameSyntax identifierName } => identifierName.Identifier.ValueText,
                    _ => null,
                };
            }
        }
    }

    private static ExpressionStatementSyntax? Assignment(ParameterSyntax? parameter, SyntaxList<StatementSyntax> statements)
    {
        if (parameter is null)
        {
            return null;
        }

        foreach (var statement in statements)
        {
            if (statement is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Right: IdentifierNameSyntax right } } expressionStatement &&
                right.Identifier.ValueText == parameter.Identifier.ValueText)
            {
                return expressionStatement;
            }
        }

        return null;
    }

    private readonly struct Move<T>
        where T : SyntaxNode
    {
        internal readonly T From;
        internal readonly T To;

        internal Move(T @from, T to)
        {
            this.From = @from;
            this.To = to;
        }
    }
}
