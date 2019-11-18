namespace Gu.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Globalization;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0060EnumMemberValueConflictsWithAnother : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptors.GU0060EnumMemberValueConflictsWithAnother);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(this.HandleEnumMember, SyntaxKind.EnumDeclaration);
        }

        private static bool IsDerivedFromOtherEnumMembers(EnumMemberDeclarationSyntax enumMember, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (enumMember is { EqualsValue: { Value: { } value } } &&
                !value.IsKind(SyntaxKind.NumericLiteralExpression))
            {
                foreach (var node in value.DescendantNodesAndSelf())
                {
                    switch (node)
                    {
                        case IdentifierNameSyntax identifier
                            when semanticModel.GetSymbolSafe(identifier, cancellationToken) is { } symbol &&
                                 symbol.ContainingType.TypeKind != TypeKind.Enum:
                            return false;
                        case LiteralExpressionSyntax _:
                            return false;
                    }
                }

                return true;
            }

            return false;
        }

        private static bool HasFlagsAttribute(INamedTypeSymbol enumType)
        {
            return enumType.GetAttributes()
                  .TryFirst(attr => attr.AttributeClass == KnownSymbol.FlagsAttribute, out var _);
        }

        // unboxes a boxed integral value to ulong, regardless of the original boxed type
        // negative values are converted to their U2 representation
        // see http://stackoverflow.com/a/10022661/1012936
        private static ulong UnboxUMaxInt(object a)
        {
            return a is ulong ? (ulong)a : (ulong)Convert.ToInt64(a, CultureInfo.InvariantCulture);
        }

        private static void HandleNonFlagEnumMember(SyntaxNodeAnalysisContext context, EnumDeclarationSyntax enumDeclaration)
        {
            using var enumValuesSet = PooledSet<ulong>.Borrow();
            foreach (var enumMember in enumDeclaration.Members)
            {
                var symbol = context.SemanticModel.GetDeclaredSymbol(enumMember, context.CancellationToken);
                var notDerivedFromOther = !IsDerivedFromOtherEnumMembers(enumMember, context.SemanticModel, context.CancellationToken);
                var value = UnboxUMaxInt(symbol.ConstantValue);
                if (notDerivedFromOther && enumValuesSet.Contains(value))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0060EnumMemberValueConflictsWithAnother, enumMember.GetLocation()));
                }

                enumValuesSet.Add(value);
            }
        }

        private static void HandleFlagEnumMember(SyntaxNodeAnalysisContext context, EnumDeclarationSyntax enumDeclaration)
        {
            ulong bitSumOfLiterals = 0;
            foreach (var enumMember in enumDeclaration.Members)
            {
                var symbol = context.SemanticModel.GetDeclaredSymbol(enumMember, context.CancellationToken);
                var notDerivedFromOther =
                    !IsDerivedFromOtherEnumMembers(enumMember, context.SemanticModel, context.CancellationToken);
                var value = UnboxUMaxInt(symbol.ConstantValue);
                if (notDerivedFromOther && (bitSumOfLiterals & value) != 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0060EnumMemberValueConflictsWithAnother, enumMember.GetLocation()));
                }

                bitSumOfLiterals |= value;
            }
        }

        private void HandleEnumMember(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var enumDeclaration = (EnumDeclarationSyntax)context.Node;
            if (context.SemanticModel.GetDeclaredSymbolSafe(enumDeclaration, context.CancellationToken) is INamedTypeSymbol enumSymbol)
            {
                var hasFlagsAttribute = HasFlagsAttribute(enumSymbol);

                if (hasFlagsAttribute)
                {
                    HandleFlagEnumMember(context, enumDeclaration);
                }
                else
                {
                    HandleNonFlagEnumMember(context, enumDeclaration);
                }
            }
        }
    }
}
