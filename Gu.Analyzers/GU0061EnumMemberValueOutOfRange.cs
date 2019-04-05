namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0061EnumMemberValueOutOfRange : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0061";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Enum member value out of range.",
            messageFormat: "Enum member value will overflow",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "The enum member value will overflow at runtime. Probably not intended. Change enum type to long (int is default)",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(this.HandleEnumMember, SyntaxKind.EnumMemberDeclaration);
        }

        private void HandleEnumMember(SyntaxNodeAnalysisContext context)
        {
            var enumMemberDeclaration = (EnumMemberDeclarationSyntax)context.Node;
            var enumType = context.ContainingSymbol?.ContainingSymbol as INamedTypeSymbol;
            if (enumType?.EnumUnderlyingType.SpecialType != SpecialType.System_Int32)
            {
                return;
            }

            if (!(enumMemberDeclaration.EqualsValue?.Value is BinaryExpressionSyntax leftShiftExpression))
            {
                return;
            }

            if (leftShiftExpression.Kind() != SyntaxKind.LeftShiftExpression)
            {
                return;
            }

            if (!(leftShiftExpression.Left is LiteralExpressionSyntax literalExpression))
            {
                return;
            }

            if (!(literalExpression.Token.Value is int intValue))
            {
                return;
            }

            if (intValue != 1)
            {
                return;
            }

            if (leftShiftExpression.Kind() != SyntaxKind.LeftShiftExpression)
            {
                return;
            }

            if (!(leftShiftExpression.Right is LiteralExpressionSyntax literalExpressionRight))
            {
                return;
            }

            if (!(literalExpressionRight.Token.Value is int intValueRight))
            {
                return;
            }

            if (intValueRight > 30)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, literalExpression.GetLocation()));
            }
        }
    }
}
