namespace Gu.Analyzers
{
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
        private static readonly ConstantPatternSyntax True = SyntaxFactory.ConstantPattern(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression));
        private static readonly ConstantPatternSyntax False = SyntaxFactory.ConstantPattern(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression));

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
                if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out ExpressionSyntax? expression))
                {
                    if (diagnostic.AdditionalLocations.Count == 0)
                    {
                        if (Parse(expression) is var (member, property, pattern))
                        {
                            context.RegisterCodeFix(
                                $"{member} is {{ {property}: {pattern} }}",
                                (e, c) => e.ReplaceNode(
                                    expression,
                                    SyntaxFactory.IsPatternExpression(
                                        member,
                                        SyntaxFactory.RecursivePattern(
                                            null,
                                            null,
                                            SyntaxFactory.PropertyPatternClause(SyntaxFactory.SingletonSeparatedList(PropertyPattern(property, pattern))),
                                            null))),
                                "Use pattern",
                                diagnostic);
                        }
                    }
                    else if (diagnostic.AdditionalLocations.TrySingle(out var additionalLocation) &&
                             syntaxRoot.TryFindNode(additionalLocation, out IsPatternExpressionSyntax? left) &&
                             expression.TryFindSharedAncestorRecursive(left, out BinaryExpressionSyntax? binary) &&
                             Parse(expression) is var (member, property, pattern))
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
                                return isPattern.WithPattern(
                                    recursive.AddPropertyPatternClauseSubpatterns(
                                        PropertyPattern(
                                            property,
                                            pattern)));
                            }

                            return old;
                        }
                    }
                }
            }
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
                    => (m, p, SyntaxFactory.ConstantPattern(c)),
                BinaryExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax m, Name: IdentifierNameSyntax p }, OperatorToken: { ValueText: "!=" }, Right: LiteralExpressionSyntax { Token: { ValueText: "null" } } }
                    => (m, p, Empty),
                IsPatternExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax m, Name: IdentifierNameSyntax p }, Pattern: ConstantPatternSyntax c }
                    => (m, p, c),
                _ => default,
            };
        }

        private static SubpatternSyntax PropertyPattern(IdentifierNameSyntax property, PatternSyntax constant)
        {
            return SyntaxFactory.Subpattern(
                nameColon: SyntaxFactory.NameColon(property),
                pattern: constant);
        }
    }
}
