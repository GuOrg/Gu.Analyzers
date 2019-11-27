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
            type: default,
            positionalPatternClause: default,
            propertyPatternClause: SyntaxFactory.PropertyPatternClause(
                openBraceToken: SyntaxFactory.Token(
                    leading: default,
                    kind: SyntaxKind.OpenBraceToken,
                    trailing: SyntaxFactory.TriviaList(SyntaxFactory.Space)),
                subpatterns: default,
                closeBraceToken: SyntaxFactory.Token(
                    leading: default,
                    kind: SyntaxKind.CloseBraceToken,
                    trailing: SyntaxFactory.TriviaList(SyntaxFactory.Space))),
            designation: default);

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
                    else if (diagnostic.AdditionalLocations.TrySingle(out var additionalLocation) &&
                             syntaxRoot.TryFindNode(additionalLocation, out PatternSyntax? mergeWith))
                    {
                        context.RegisterCodeFix(
                            mergeWith is RecursivePatternSyntax
                                ? $", {property}: {pattern}"
                                : $"{{ {property}: {pattern} }}",
                            (e, c) =>
                            {
                                _ = e.ReplaceNode(mergeWith, x => Merge(x));
                                switch (expression.Parent)
                                {
                                    case WhenClauseSyntax whenClause:
                                        e.RemoveNode(whenClause);
                                        break;
                                    case BinaryExpressionSyntax binary:
                                        if (binary.Left == expression)
                                        {
                                            _ = e.ReplaceNode(
                                                binary,
                                                x => x.Right.WithTrailingTrivia(SyntaxFactory.ElasticSpace));
                                        }

                                        if (binary.Right == expression)
                                        {
                                            _ = e.ReplaceNode(
                                                binary,
                                                x => x.Left.WithTrailingTrivia(SyntaxFactory.ElasticSpace));
                                        }

                                        break;
                                }
                            },
                            "Use pattern",
                            diagnostic);

                        RecursivePatternSyntax Merge(PatternSyntax old)
                        {
                            return old switch
                            {
                                RecursivePatternSyntax recursive
                                    => AddProperty(recursive, property, pattern),
                                DeclarationPatternSyntax declaration
                                    => AddProperty(declaration, property, pattern),
                                _ => throw new NotSupportedException(),
                            };
                        }
                    }
                }
            }
        }

        private static RecursivePatternSyntax AddProperty(RecursivePatternSyntax recursive, IdentifierNameSyntax property, PatternSyntax pattern)
        {
            return recursive.AddPropertyPatternClauseSubpatterns(PropertyPattern(property, pattern));
        }

        private static RecursivePatternSyntax AddProperty(DeclarationPatternSyntax declaration, IdentifierNameSyntax property, PatternSyntax pattern)
        {
            return SyntaxFactory.RecursivePattern(
                    type: declaration.Type,
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
                                pattern: pattern)),
                        closeBraceToken: SyntaxFactory.Token(
                            leading: default,
                            kind: SyntaxKind.CloseBraceToken,
                            trailing: SyntaxFactory.TriviaList(SyntaxFactory.ElasticSpace))),
                    designation: declaration.Designation);
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
                IsPatternExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax m, Name: IdentifierNameSyntax p }, Pattern: DeclarationPatternSyntax { Designation: SingleVariableDesignationSyntax _ } c }
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
