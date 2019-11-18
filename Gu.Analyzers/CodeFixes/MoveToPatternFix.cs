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
                        switch (expression)
                        {
                            case MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax member, Name: IdentifierNameSyntax property }:
                                context.RegisterCodeFix(
                                    $"{member} is {{ {property}: true }}",
                                    (e, c) => e.ReplaceNode(
                                        expression,
                                        SyntaxFactory.IsPatternExpression(
                                            member,
                                            SyntaxFactory.RecursivePattern(
                                                null,
                                                null,
                                                SyntaxFactory.PropertyPatternClause(
                                                    SyntaxFactory.SingletonSeparatedList(
                                                        PropertyPattern(
                                                            property,
                                                            SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)))),
                                                null))),
                                    "Use pattern",
                                    diagnostic);
                                break;
                            case PrefixUnaryExpressionSyntax { Operand: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax member, Name: IdentifierNameSyntax property } }:
                                context.RegisterCodeFix(
                                    $"{member} is {{ {property}: false }}",
                                    (e, c) => e.ReplaceNode(
                                        expression,
                                        SyntaxFactory.IsPatternExpression(
                                            member,
                                            SyntaxFactory.RecursivePattern(
                                                null,
                                                null,
                                                SyntaxFactory.PropertyPatternClause(
                                                    SyntaxFactory.SingletonSeparatedList(
                                                        PropertyPattern(
                                                            property,
                                                            SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)))),
                                                null))),
                                    "Use pattern",
                                    diagnostic);
                                break;
                        }
                    }
                    else if (diagnostic.AdditionalLocations.TrySingle(out var additionalLocation) &&
                             syntaxRoot.TryFindNode(additionalLocation, out IsPatternExpressionSyntax? pattern) &&
                             expression.TryFindSharedAncestorRecursive(pattern, out BinaryExpressionSyntax? binary))
                    {
                        context.RegisterCodeFix(
                            $"Merge {binary.Right}.",
                            (e, c) => e.ReplaceNode(
                                binary,
                                x => Merge(x)),
                            "Use pattern",
                            diagnostic);

                        ExpressionSyntax Merge(BinaryExpressionSyntax old)
                        {
                            if (old.Left is IsPatternExpressionSyntax { Pattern: RecursivePatternSyntax recursive } isPattern)
                            {
                                switch (old.Right)
                                {
                                    case MemberAccessExpressionSyntax { Name: IdentifierNameSyntax right }:
                                        return isPattern.WithPattern(
                                            recursive.AddPropertyPatternClauseSubpatterns(
                                                PropertyPattern(
                                                    right,
                                                    SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression))));
                                    case PrefixUnaryExpressionSyntax { Operand: MemberAccessExpressionSyntax { Name: IdentifierNameSyntax right } }:
                                        return isPattern.WithPattern(
                                            recursive.AddPropertyPatternClauseSubpatterns(
                                                PropertyPattern(
                                                    right,
                                                    SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))));
                                }
                            }

                            return old;
                        }
                    }
                }

                static SubpatternSyntax PropertyPattern(IdentifierNameSyntax identifierName, LiteralExpressionSyntax constant)
                {
                    return SyntaxFactory.Subpattern(
                        nameColon: SyntaxFactory.NameColon(identifierName),
                        pattern: SyntaxFactory.ConstantPattern(constant));
                }
            }
        }
    }
}
