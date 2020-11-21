namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;

    using Gu.Roslyn.CodeFixExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AssertFix))]
    [Shared]
    internal class AssertFix : DocumentEditorCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            Descriptors.GU0084AssertExceptionMessage.Id);

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot is { } &&
                    syntaxRoot.TryFindNode(diagnostic, out InvocationExpressionSyntax? invocation) &&
                    Statement() is { } statement)
                {
                    context.RegisterCodeFix(
                        "Assert exception message inline.",
                        editor =>
                        {
                            editor
                                .ReplaceNode(
                                    statement,
                                    AssertMessage(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            invocation,
                                            SyntaxFactory.IdentifierName("Message"))));
                        },
                        "Assert exception message inline.",
                        diagnostic);

                    context.RegisterCodeFix(
                        "Assert exception message via local variable.",
                        editor =>
                        {
                            editor.InsertAfter(
                                statement,
                                AssertMessage(
                                    SyntaxFactory.MemberAccessExpression(
                                        kind: SyntaxKind.SimpleMemberAccessExpression,
                                        expression: SyntaxFactory.IdentifierName("exception"),
                                        name: SyntaxFactory.IdentifierName("Message"))));
                            editor
                                .ReplaceNode(
                                    statement,
                                    SyntaxFactory.LocalDeclarationStatement(
                                        declaration: SyntaxFactory.VariableDeclaration(
                                            type: SyntaxFactory.IdentifierName("var"),
                                            variables: SyntaxFactory
                                                .SingletonSeparatedList(
                                                    SyntaxFactory.VariableDeclarator(
                                                        identifier: SyntaxFactory.Identifier(
                                                            "exception"),
                                                        argumentList: default,
                                                        initializer: SyntaxFactory
                                                            .EqualsValueClause(
                                                                value: invocation))))));
                        },
                        "Assert exception message via local variable.",
                        diagnostic);
                }

                ExpressionStatementSyntax? Statement()
                {
                    return invocation.Parent switch
                    {
                        ExpressionStatementSyntax s => s,
                        AssignmentExpressionSyntax { Parent: ExpressionStatementSyntax s } => s,
                        _ => null,
                    };
                }

                ExpressionStatementSyntax AssertMessage(ExpressionSyntax exception)
                {
                    return SyntaxFactory.ExpressionStatement(
                        expression: SyntaxFactory.InvocationExpression(
                            expression: SyntaxFactory.MemberAccessExpression(
                                kind: SyntaxKind.SimpleMemberAccessExpression,
                                expression: SyntaxFactory.IdentifierName("Assert"),
                                name: SyntaxFactory.IdentifierName("AreEqual")),
                            argumentList: SyntaxFactory.ArgumentList(
                                arguments: SyntaxFactory.SeparatedList(
                                    new[]
                                    {
                                        SyntaxFactory.Argument(
                                            expression: SyntaxFactory.LiteralExpression(
                                                kind: SyntaxKind.StringLiteralExpression,
                                                token: SyntaxFactory.Literal(
                                                    text: "\"EXPECTED\"",
                                                    value: "EXPECTED"))),
                                        SyntaxFactory.Argument(
                                            expression: exception),
                                    },
                                    new[] { SyntaxFactory.Token(SyntaxKind.CommaToken) }))),
                        semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                }
            }
        }
    }
}
