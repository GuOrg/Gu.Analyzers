namespace Gu.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0060EnumMemberValueConflictsWithAnother : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0060";
        private const string Title = "Enum member value conflict.";
        private const string MessageFormat = "Enum member value conflicts with another.";
        private const string Description = "The enum member has a value shared with the other enum member, but it's not explicitly declared as its alias. To fix this, assign a enum member";
        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: Title,
            messageFormat: MessageFormat,
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: Description,
            helpLinkUri: HelpLink);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(this.HandleEnumMember, SyntaxKind.EnumDeclaration);
        }

        private static bool HasFlagsAttribute(INamedTypeSymbol enumType)
        {
            return enumType.GetAttributes()
                  .TryGetFirst(attr => attr.AttributeClass == KnownSymbol.FlagsAttribute, out AttributeData _);
        }

        // unboxes a boxed integral value to ulong, regardless of the original boxed type
        // negative values are converted to their U2 representation
        // see http://stackoverflow.com/a/10022661/1012936
        private static ulong UnboxUMaxInt(object a)
        {
            return a is ulong ? (ulong)a : (ulong)Convert.ToInt64(a);
        }

        private void HandleEnumMember(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var enumDeclaration = (EnumDeclarationSyntax)context.Node;
            var enumSymbol = context.SemanticModel.GetDeclaredSymbolSafe(enumDeclaration, context.CancellationToken) as INamedTypeSymbol;
            if (enumSymbol == null)
            {
                return;
            }

            var hasFlagsAttribute = HasFlagsAttribute(enumSymbol);
            var enumMembers = enumDeclaration.Members;

            if (!hasFlagsAttribute)
            {
                return;
            }

            ulong bitSumOfLiterals = 0;
            foreach (var enumMember in enumMembers)
            {
                var symbol = context.SemanticModel.GetDeclaredSymbol(enumMember, context.CancellationToken);
                bool isLiteralOrImplicit = enumMember.EqualsValue == null ||
                                         enumMember.EqualsValue.Value is LiteralExpressionSyntax;
                var value = UnboxUMaxInt(symbol.ConstantValue);
                if (isLiteralOrImplicit && (bitSumOfLiterals & value) != 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, enumMember.GetLocation()));
                }

                bitSumOfLiterals |= value;
            }
        }
    }
}