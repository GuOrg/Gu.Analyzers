namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Linq.Expressions;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class WhenAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptors.GU0074PreferPattern);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => AnalyzeNode(c), SyntaxKind.WhenClause);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is WhenClauseSyntax whenClause)
            {
                if (Pattern() is { } pattern)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            Descriptors.GU0074PreferPattern,
                            whenClause.Condition.GetLocation(),
                            additionalLocations: new[] { pattern.GetLocation() }));
                }

                PatternSyntax? Pattern()
                {
                    if (Expression(whenClause.Condition) is { } expression)
                    {
                        return whenClause switch
                        {
                            { Parent: SwitchExpressionArmSyntax { Pattern: RecursivePatternSyntax { Designation: SingleVariableDesignationSyntax designation } pattern, Parent: SwitchExpressionSyntax _ } }
                                when AreSame(expression, designation)
                                    => pattern,
                            { Parent: SwitchExpressionArmSyntax { Pattern: RecursivePatternSyntax pattern, Parent: SwitchExpressionSyntax switchExpression } }
                                when AreSame(expression, switchExpression.GoverningExpression)
                                    => pattern,
                            { Parent: CasePatternSwitchLabelSyntax { Pattern: RecursivePatternSyntax { Designation: SingleVariableDesignationSyntax designation } pattern, Parent: SwitchSectionSyntax { Parent: SwitchStatementSyntax _ } } }
                                when AreSame(expression, designation)
                                    => pattern,
                            { Parent: CasePatternSwitchLabelSyntax { Pattern: DeclarationPatternSyntax { Designation: SingleVariableDesignationSyntax designation } pattern, Parent: SwitchSectionSyntax { Parent: SwitchStatementSyntax _ } } }
                                when AreSame(expression, designation)
                                    => pattern,
                            { Parent: CasePatternSwitchLabelSyntax { Pattern: RecursivePatternSyntax pattern, Parent: SwitchSectionSyntax { Parent: SwitchStatementSyntax switchStatement } } }
                                when AreSame(expression, switchStatement.Expression)
                                    => pattern,
                            _ => null,
                        };
                    }

                    return null;
                }

                static bool AreSame(ExpressionSyntax x, SyntaxNode y)
                {
                    return (x, y) switch
                    {
                        { x: IdentifierNameSyntax xn, y: IdentifierNameSyntax yn } => xn.Identifier.ValueText == yn.Identifier.ValueText,
                        { x: IdentifierNameSyntax xn, y: SingleVariableDesignationSyntax yn } => xn.Identifier.ValueText == yn.Identifier.ValueText,
                        _ => false,
                    };
                }

                static ExpressionSyntax? Expression(ExpressionSyntax candidate)
                {
                    return candidate switch
                    {
                        MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax e, Name: IdentifierNameSyntax _ }
                            => e,
                        PrefixUnaryExpressionSyntax { Operand: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax e, Name: IdentifierNameSyntax _ } }
                            => e,
                        BinaryExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax e, Name: IdentifierNameSyntax _ }, OperatorToken: { ValueText: "==" }, Right: LiteralExpressionSyntax _ }
                            => e,
                        BinaryExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax e, Name: IdentifierNameSyntax _ }, OperatorToken: { ValueText: "!=" }, Right: LiteralExpressionSyntax { Token: { ValueText: "null" } } }
                            => e,
                        IsPatternExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax e, Name: IdentifierNameSyntax _ }, Pattern: ConstantPatternSyntax _ }
                            => e,
                        _ => null,
                    };
                }
            }
        }
    }
}
