namespace Gu.Analyzers
{
    using System.Collections.Immutable;

    using Gu.Roslyn.AnalyzerExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0061EnumMemberValueOutOfRange : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
                Descriptors.GU0061EnumMemberValueOutOfRange);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(this.HandleEnumMember, SyntaxKind.EnumMemberDeclaration);
        }

        private void HandleEnumMember(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is EnumMemberDeclarationSyntax { EqualsValue: { Value: BinaryExpressionSyntax { Left: LiteralExpressionSyntax { Token: { Value: 1 } }, OperatorToken: { ValueText: "<<" }, Right: LiteralExpressionSyntax right } leftShiftExpression } } &&
                context.ContainingSymbol is { ContainingType: { EnumUnderlyingType: { SpecialType: SpecialType.System_Int32 } } } &&
                right is { Token: { Value: int intValueRight } } &&
                intValueRight > 30)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0061EnumMemberValueOutOfRange, leftShiftExpression.GetLocation()));
            }
        }
    }
}
