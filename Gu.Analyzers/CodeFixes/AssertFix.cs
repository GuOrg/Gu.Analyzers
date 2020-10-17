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
                if (syntaxRoot.TryFindNode(diagnostic, out InvocationExpressionSyntax? invocation) &&
                    Statement() is { } statement)
                {
                    context.RegisterCodeFix(
                        "Assert exception message.",
                        (editor, cancellationToken) =>
                        {
                            editor.InsertAfter(
                                statement,
                                AssertMessage());
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
                        nameof(AssertFix),
                        diagnostic);
                }

                StatementSyntax? Statement()
                {
                    return invocation.Parent switch
                    {
                        ExpressionStatementSyntax statement => statement,
                        AssignmentExpressionSyntax { Parent: ExpressionStatementSyntax statement } => statement,
                        _ => null,
                    };
                }
            }

            ExpressionStatementSyntax AssertMessage()
            {
                return SyntaxFactory.ExpressionStatement(
                    expression: SyntaxFactory.InvocationExpression(
                        expression: SyntaxFactory.MemberAccessExpression(
                            kind: SyntaxKind.SimpleMemberAccessExpression,
                            expression: SyntaxFactory.IdentifierName("Assert"),
                            name: SyntaxFactory.IdentifierName("AreEqual")),
                        argumentList: SyntaxFactory.ArgumentList(
                            arguments: SyntaxFactory.SeparatedList(
                                new ArgumentSyntax[]
                                {
                                    SyntaxFactory.Argument(
                                        nameColon: default,
                                        refKindKeyword: default,
                                        expression: SyntaxFactory.LiteralExpression(
                                            kind: SyntaxKind.StringLiteralExpression,
                                            token: SyntaxFactory.Literal(
                                                text: "\"EXPECTED\"",
                                                value: "EXPECTED"))),
                                    SyntaxFactory.Argument(
                                        nameColon: default,
                                        refKindKeyword: default,
                                        expression: SyntaxFactory.MemberAccessExpression(
                                            kind: SyntaxKind.SimpleMemberAccessExpression,
                                            expression: SyntaxFactory.IdentifierName("exception"),
                                            name: SyntaxFactory.IdentifierName("Message"))),
                                },
                                new[] { SyntaxFactory.Token(SyntaxKind.CommaToken) }))),
                    semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            }
        }
    }
}
