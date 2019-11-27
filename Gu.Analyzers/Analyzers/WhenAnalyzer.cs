namespace Gu.Analyzers
{
    using System.Collections.Immutable;
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
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.WhenClause);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is WhenClauseSyntax whenClause &&
                Pattern.Identifier(whenClause.Condition) is { } identifier &&
                MergePattern(identifier, whenClause) is { } pattern)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.GU0074PreferPattern,
                        whenClause.Condition.GetLocation(),
                        additionalLocations: new[] { pattern.GetLocation() }));
            }
        }

        private static PatternSyntax? MergePattern(IdentifierNameSyntax identifier, WhenClauseSyntax whenClause)
        {
            return whenClause switch
            {
                { Parent: SwitchExpressionArmSyntax { Pattern: { } pattern, Parent: SwitchExpressionSyntax _ } }
                when Pattern.MergePattern(identifier, pattern) is { } mergePattern
                => mergePattern,
                { Parent: SwitchExpressionArmSyntax { Pattern: RecursivePatternSyntax pattern, Parent: SwitchExpressionSyntax switchExpression } }
                when AreSame(identifier, switchExpression.GoverningExpression)
                => pattern,
                { Parent: CasePatternSwitchLabelSyntax { Pattern: { } pattern, Parent: SwitchSectionSyntax { Parent: SwitchStatementSyntax _ } } }
                when Pattern.MergePattern(identifier, pattern) is { } mergePattern
                => mergePattern,
                { Parent: CasePatternSwitchLabelSyntax { Pattern: RecursivePatternSyntax pattern, Parent: SwitchSectionSyntax { Parent: SwitchStatementSyntax switchStatement } } }
                when AreSame(identifier, switchStatement.Expression)
                => pattern,
                _ => null,
            };

            static bool AreSame(ExpressionSyntax x, SyntaxNode y)
            {
                return (x, y) switch
                {
                    { x: IdentifierNameSyntax xn, y: IdentifierNameSyntax yn } => xn.Identifier.ValueText == yn.Identifier.ValueText,
                    { x: IdentifierNameSyntax xn, y: SingleVariableDesignationSyntax yn } => xn.Identifier.ValueText == yn.Identifier.ValueText,
                    _ => false,
                };
            }
        }
    }
}
