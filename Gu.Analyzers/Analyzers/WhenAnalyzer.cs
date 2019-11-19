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
            context.RegisterSyntaxNodeAction(c => AnalyzeNode(c), SyntaxKind.WhenClause);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is WhenClauseSyntax whenClause)
            {
                switch (whenClause.Parent)
                {
                    case SwitchExpressionArmSyntax { Pattern: RecursivePatternSyntax { Designation: SingleVariableDesignationSyntax designation } pattern }:
                        if (whenClause.Condition is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax expression, Name: IdentifierNameSyntax _ } memberAccess &&
                            expression.Identifier.ValueText == designation.Identifier.ValueText)
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    Descriptors.GU0074PreferPattern,
                                    memberAccess.GetLocation(),
                                    additionalLocations: new[] { pattern.GetLocation() }));
                        }

                        break;
                }
            }
        }
    }
}
