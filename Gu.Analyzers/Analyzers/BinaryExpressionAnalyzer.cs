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
                switch (binaryExpression)
                {
                    case { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax _, Name: IdentifierNameSyntax _ } left }:
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                Descriptors.GU0074PreferPattern,
                                left.GetLocation()));
                        break;

                    case { Left: IsPatternExpressionSyntax { Expression: IdentifierNameSyntax leftMember, Pattern: RecursivePatternSyntax _ } isPattern, Right: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax rightMember, Name: IdentifierNameSyntax _ } right }
                        when leftMember.Identifier.ValueText == rightMember.Identifier.ValueText:
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                Descriptors.GU0074PreferPattern,
                                right.GetLocation(),
                                additionalLocations: new[] { isPattern.GetLocation() }));
                        break;
                }
            }
        }
    }
}
