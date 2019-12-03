namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullCheckParameterFix))]
    [Shared]
    internal class NullCheckParameterFix : DocumentEditorCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            Descriptors.GU0012NullCheckParameter.Id,
            "CA1062");

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);

                if (diagnostic.Id == "CA1062")
                {
                    if (node.TryFirstAncestor<BaseMethodDeclarationSyntax>(out var method) &&
                        method.TryFindParameter(node.ToString(), out var parameter))
                    {
                        node = parameter;
                    }
                    else
                    {
                        continue;
                    }
                }

                if (node is IdentifierNameSyntax identifierName)
                {
                    context.RegisterCodeFix(
                        "Throw if null.",
                        (editor, _) => editor.ReplaceNode(
                            node,
                            SyntaxFactory.ParseExpression($"{identifierName.Identifier.Text} ?? throw new System.ArgumentNullException(nameof({identifierName.Identifier.Text}))").WithSimplifiedNames()),
                        this.GetType(),
                        diagnostic);
                }
                else if (node is ParameterSyntax parameter)
                {
                    var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                                     .ConfigureAwait(false);
                    if (parameter.Parent is ParameterListSyntax parameterList &&
                        parameterList.Parent is BaseMethodDeclarationSyntax methodDeclaration)
                    {
                        using var walker = AssignmentExecutionWalker.Borrow(methodDeclaration, SearchScope.Member, semanticModel, context.CancellationToken);
                        if (TryFirstAssignedWith(parameter, walker.Assignments, out var assignedValue) &&
                            semanticModel.TryGetSymbol(parameter, context.CancellationToken, out var parameterSymbol) &&
                            IdentifierNameWalker.TryFindFirst(methodDeclaration, parameterSymbol, semanticModel, context.CancellationToken, out var first) &&
                            assignedValue.Contains(first))
                        {
                            context.RegisterCodeFix(
                                "Throw if null on first assignment.",
                                (editor, _) => editor.ReplaceNode(
                                    assignedValue,
                                    SyntaxFactory.ParseExpression($"{assignedValue.Identifier.Text} ?? throw new System.ArgumentNullException(nameof({parameter.Identifier.Text}))").WithSimplifiedNames()),
                                diagnostic.Id,
                                diagnostic);
                        }
                        else if (methodDeclaration.Body is { } block)
                        {
                            context.RegisterCodeFix(
                                "Add null check.",
                                (editor, cancellationToken) => editor.ReplaceNode(
                                    block,
                                    x => WithNullCheck(x, cancellationToken)),
                                diagnostic.Id,
                                diagnostic);
                        }
                        else if (methodDeclaration is { ExpressionBody: { Expression: { } expression } })
                        {
                            context.RegisterCodeFix(
                                "Add null check.",
                                (editor, cancellationToken) => editor.ReplaceNode(
                                    methodDeclaration,
                                    x => x.WithExpressionBody(null)
                                          .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                                          .WithBody(
                                              SyntaxFactory.Block(
                                                        IfNullThrow(),
                                                        Statement()))),
                                diagnostic.Id,
                                diagnostic);

                            StatementSyntax Statement()
                            {
                                return methodDeclaration switch
                                {
                                    MethodDeclarationSyntax { ReturnType: PredefinedTypeSyntax { Keyword: { ValueText: "void" } } } => SyntaxFactory.ExpressionStatement(expression),
                                    MethodDeclarationSyntax _ => SyntaxFactory.ReturnStatement(expression),
                                    _ => SyntaxFactory.ExpressionStatement(expression),
                                };
                            }
                        }

                        IfStatementSyntax IfNullThrow()
                        {
                            return SyntaxFactory.IfStatement(
                                                    SyntaxFactory.IsPatternExpression(
                                                        SyntaxFactory.ParseName(parameter.Identifier.Text),
                                                        SyntaxFactory.ConstantPattern(
                                                            SyntaxFactory.LiteralExpression(
                                                                SyntaxKind.NullLiteralExpression))),
                                                    SyntaxFactory.Block(
                                                        SyntaxFactory.ThrowStatement(
                                                            SyntaxFactory.ParseExpression(
                                                                $"new System.ArgumentNullException(nameof({parameter.Identifier.Text}))").WithSimplifiedNames())))
                                                .WithLeadingElasticLineFeed()
                                                .WithTrailingLineFeed();
                        }

                        BlockSyntax WithNullCheck(BlockSyntax body, CancellationToken cancellationToken)
                        {
                            return body.WithStatements(body.Statements.Insert(FindPosition(), IfNullThrow()));

                            int FindPosition()
                            {
                                var ordinal = parameterList.Parameters.IndexOf(parameter);
                                if (ordinal <= 0)
                                {
                                    return 0;
                                }

                                var position = 0;
                                foreach (var statement in body.Statements)
                                {
                                    if (statement is IfStatementSyntax ifStatement &&
                                        IsThrow(ifStatement.Statement) &&
                                        NullCheck.IsNullCheck(ifStatement.Condition, null, cancellationToken, out var value) &&
                                        value is IdentifierNameSyntax nullChecked &&
                                        parameterList.TryFind(nullChecked.Identifier.ValueText, out var other) &&
                                        parameterList.Parameters.IndexOf(other) < ordinal)
                                    {
                                        position++;
                                    }
                                    else
                                    {
                                        return position;
                                    }
                                }

                                return position;
                            }
                        }
                    }
                }
            }
        }

        private static bool TryFirstAssignedWith(ParameterSyntax parameter, IReadOnlyList<AssignmentExpressionSyntax> assignments, [NotNullWhen(true)] out IdentifierNameSyntax? assignedValue)
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

        private static bool IsThrow(StatementSyntax statement)
        {
            if (statement is ThrowStatementSyntax)
            {
                return true;
            }

            return statement is BlockSyntax block &&
                   block.Statements.TrySingle(out var single) &&
                   single is ThrowStatementSyntax;
        }
    }
}
