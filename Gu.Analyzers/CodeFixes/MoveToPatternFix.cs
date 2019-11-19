namespace Gu.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MoveToPatternFix))]
    [Shared]
    internal class MoveToPatternFix : DocumentEditorCodeFixProvider
    {
        private static readonly ConstantPatternSyntax True = ConstantPattern(SyntaxKind.TrueLiteralExpression, SyntaxKind.TrueKeyword);

        private static readonly ConstantPatternSyntax False = ConstantPattern(SyntaxKind.FalseLiteralExpression, SyntaxKind.FalseKeyword);

        private static readonly RecursivePatternSyntax Empty = SyntaxFactory.RecursivePattern(
            null,
            null,
            SyntaxFactory.PropertyPatternClause(),
            null);

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            Descriptors.GU0074PreferPattern.Id);

        protected override DocumentEditorFixAllProvider? FixAllProvider() => null;

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                                   .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out ExpressionSyntax? expression) &&
                    Parse(expression) is var (member, property, pattern))
                {
                    if (diagnostic.AdditionalLocations.Count == 0)
                    {
                        context.RegisterCodeFix(
                            $"{member} is {{ {property}: {pattern} }}",
                            (e, c) => e.ReplaceNode(
                                expression,
                                IsPattern(member, property, pattern)),
                            "Use pattern",
                            diagnostic);
                    }
                    else if (diagnostic.AdditionalLocations.TrySingle(out var additionalLocation))
                    {
                        if (syntaxRoot.TryFindNode(additionalLocation, out IsPatternExpressionSyntax? left) &&
                            expression.TryFindSharedAncestorRecursive(left, out BinaryExpressionSyntax? binary))
                        {
                            context.RegisterCodeFix(
                                $"Merge {{ {property}: {pattern} }}",
                                (e, c) => e.ReplaceNode(
                                    binary,
                                    x => Merge(x)),
                                "Use pattern",
                                diagnostic);

                            ExpressionSyntax Merge(BinaryExpressionSyntax old)
                            {
                                if (old.Left is IsPatternExpressionSyntax { Pattern: RecursivePatternSyntax recursive } isPattern)
                                {
                                    return isPattern.WithPattern(AddProperty(recursive, property, pattern))
                                                    .WithTrailingTrivia(SyntaxFactory.ElasticSpace);
                                }

                                return old;
                            }
                        }
                        else if (syntaxRoot.TryFindNode(additionalLocation, out RecursivePatternSyntax? recursive) &&
                                 recursive.Parent.IsKind(SyntaxKind.SwitchExpressionArm))
                        {
                            context.RegisterCodeFix(
                                $"Merge {{ {property}: {pattern} }}",
                                (e, c) =>
                                {
                                    e.ReplaceNode(
                                        recursive,
                                        x => AddProperty(x, property, pattern));
                                    if (expression.Parent is WhenClauseSyntax whenClause)
                                    {
                                        e.RemoveNode(whenClause);
                                    }
                                },
                                "Use pattern",
                                diagnostic);
                        }
                    }
                }
            }
        }

        private static RecursivePatternSyntax AddProperty(RecursivePatternSyntax recursive, IdentifierNameSyntax property, PatternSyntax pattern)
        {
            return recursive.AddPropertyPatternClauseSubpatterns(PropertyPattern(property, pattern));
        }

        private static (IdentifierNameSyntax, IdentifierNameSyntax, PatternSyntax)? Parse(ExpressionSyntax expression)
        {
            return expression switch
            {
                MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax m, Name: IdentifierNameSyntax p }
                    => (m, p, True),
                PrefixUnaryExpressionSyntax { Operand: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax m, Name: IdentifierNameSyntax p } }
                    => (m, p, False),
                BinaryExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax m, Name: IdentifierNameSyntax p }, OperatorToken: { ValueText: "==" }, Right: LiteralExpressionSyntax c }
                    => (m, p, SyntaxFactory.ConstantPattern(c.WithoutTrivia())),
                BinaryExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax m, Name: IdentifierNameSyntax p }, OperatorToken: { ValueText: "!=" }, Right: LiteralExpressionSyntax { Token: { ValueText: "null" } } }
                    => (m, p, Empty),
                IsPatternExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax m, Name: IdentifierNameSyntax p }, Pattern: ConstantPatternSyntax c }
                    => (m, p, c),
                _ => default,
            };
        }

        private static IsPatternExpressionSyntax IsPattern(IdentifierNameSyntax expression, IdentifierNameSyntax property, PatternSyntax constant)
        {
            return SyntaxFactory.IsPatternExpression(
                expression: expression.WithoutTrailingTrivia().WithTrailingTrivia(SyntaxFactory.Space),
                isKeyword: SyntaxFactory.Token(
                    leading: default,
                    kind: SyntaxKind.IsKeyword,
                    trailing: SyntaxFactory.TriviaList(SyntaxFactory.Space)),
                pattern: SyntaxFactory.RecursivePattern(
                    type: default,
                    positionalPatternClause: default,
                    propertyPatternClause: SyntaxFactory.PropertyPatternClause(
                        openBraceToken: SyntaxFactory.Token(
                            leading: default,
                            kind: SyntaxKind.OpenBraceToken,
                            trailing: SyntaxFactory.TriviaList(SyntaxFactory.Space)),
                        subpatterns: SyntaxFactory.SingletonSeparatedList<SubpatternSyntax>(
                            SyntaxFactory.Subpattern(
                                nameColon: SyntaxFactory.NameColon(
                                    name: property.WithoutTrivia(),
                                    colonToken: SyntaxFactory.Token(
                                        leading: default,
                                        kind: SyntaxKind.ColonToken,
                                        trailing: SyntaxFactory.TriviaList(SyntaxFactory.Space))),
                                pattern: constant)),
                        closeBraceToken: SyntaxFactory.Token(
                            leading: default,
                            kind: SyntaxKind.CloseBraceToken,
                            trailing: SyntaxFactory.TriviaList(SyntaxFactory.ElasticSpace))),
                    designation: default));
        }

        private static ConstantPatternSyntax ConstantPattern(SyntaxKind expressionKind, SyntaxKind keyword)
        {
            return SyntaxFactory.ConstantPattern(
                expression: SyntaxFactory.LiteralExpression(
                    kind: expressionKind,
                    token: SyntaxFactory.Token(
                        leading: default,
                        kind: keyword,
                        trailing: SyntaxFactory.TriviaList(SyntaxFactory.Space))));
        }

        private static SubpatternSyntax PropertyPattern(IdentifierNameSyntax property, PatternSyntax constant)
        {
            return SyntaxFactory.Subpattern(
                nameColon: SyntaxFactory.NameColon(property),
                pattern: constant);
        }
    }
}
