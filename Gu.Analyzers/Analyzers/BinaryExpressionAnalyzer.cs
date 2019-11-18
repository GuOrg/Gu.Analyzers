namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class BinaryExpressionAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptors.GU0074PreferPattern);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => AnalyzeNode(c), SyntaxKind.LogicalAndExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is BinaryExpressionSyntax binaryExpression)
            {
                if (ConvertToPattern(binaryExpression.Left) is { } left)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            Descriptors.GU0074PreferPattern,
                            left.GetLocation()));
                }
                else if (binaryExpression.Left is IsPatternExpressionSyntax { Expression: IdentifierNameSyntax _, Pattern: RecursivePatternSyntax _ } isPattern &&
                         ConvertToPattern(binaryExpression.Right) is { } right)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            Descriptors.GU0074PreferPattern,
                            right.GetLocation(),
                            additionalLocations: new[] { isPattern.GetLocation() }));
                }

                ExpressionSyntax? ConvertToPattern(ExpressionSyntax expression)
                {
                    return expression switch
                    {
                        MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax _, Name: IdentifierNameSyntax _ } => expression,
                        PrefixUnaryExpressionSyntax { Operand: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax _, Name: IdentifierNameSyntax _ } } => expression,
                        BinaryExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax _, Name: IdentifierNameSyntax _ }, OperatorToken: { ValueText: "==" }, Right: LiteralExpressionSyntax _ } => expression,
                        BinaryExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax _, Name: IdentifierNameSyntax _ }, OperatorToken: { ValueText: "!=" }, Right: LiteralExpressionSyntax { Token: { ValueText: "null" } } } => expression,
                        IsPatternExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax _, Name: IdentifierNameSyntax _ }, Pattern: ConstantPatternSyntax _ } => expression,
                        _ => null,
                    };
                }
            }
        }
    }
}
