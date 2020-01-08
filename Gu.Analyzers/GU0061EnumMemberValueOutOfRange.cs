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
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptors.GU0061EnumMemberValueOutOfRange);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(this.HandleEnumMember, SyntaxKind.EnumMemberDeclaration);
        }

        private void HandleEnumMember(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var enumMemberDeclaration = (EnumMemberDeclarationSyntax)context.Node;

            if (context.ContainingSymbol?.ContainingSymbol is INamedTypeSymbol enumType &&
                enumType.EnumUnderlyingType.SpecialType == SpecialType.System_Int32 &&
                enumMemberDeclaration.EqualsValue?.Value is BinaryExpressionSyntax leftShiftExpression &&
                leftShiftExpression.Kind() == SyntaxKind.LeftShiftExpression &&
                leftShiftExpression.Left is LiteralExpressionSyntax literalExpression &&
                literalExpression.Token.Value is int intValue &&
                intValue == 1 &&
                leftShiftExpression.Right is LiteralExpressionSyntax literalExpressionRight &&
                literalExpressionRight.Token.Value is int intValueRight &&
                intValueRight > 30)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0061EnumMemberValueOutOfRange, literalExpression.GetLocation()));
            }
        }
    }
}
