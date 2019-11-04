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
            if (enumMember.EqualsValue is null)
            {
                return false;
            }

            var expression = enumMember.EqualsValue.Value;
            foreach (var node in expression.DescendantNodesAndSelf())
            {
                if (!(node is ExpressionSyntax))
                {
                    continue;
                }

                if (node is LiteralExpressionSyntax)
                {
                    return false;
                }

                if (node is IdentifierNameSyntax identifier)
                {
                    bool isEnumMember = semanticModel.GetSymbolSafe(identifier, cancellationToken)
                                            .TrySingleDeclaration(cancellationToken, out EnumMemberDeclarationSyntax _);
                    if (!isEnumMember)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool HasFlagsAttribute(INamedTypeSymbol enumType)
        {
            return enumType.GetAttributes()
                  .TryFirst(attr => attr.AttributeClass == KnownSymbol.FlagsAttribute, out AttributeData _);
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
            using (var enumValuesSet = PooledSet<ulong>.Borrow())
            {
                foreach (var enumMember in enumDeclaration.Members)
                {
                    var symbol = context.SemanticModel.GetDeclaredSymbol(enumMember, context.CancellationToken);
                    bool notDerivedFromOther =
                        !IsDerivedFromOtherEnumMembers(enumMember, context.SemanticModel, context.CancellationToken);
                    var value = UnboxUMaxInt(symbol.ConstantValue);
                    if (notDerivedFromOther && enumValuesSet.Contains(value))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0060EnumMemberValueConflictsWithAnother, enumMember.GetLocation()));
                    }

                    enumValuesSet.Add(value);
                }
            }
        }

        private static void HandleFlagEnumMember(SyntaxNodeAnalysisContext context, EnumDeclarationSyntax enumDeclaration)
        {
            ulong bitSumOfLiterals = 0;
            foreach (var enumMember in enumDeclaration.Members)
            {
                var symbol = context.SemanticModel.GetDeclaredSymbol(enumMember, context.CancellationToken);
                bool notDerivedFromOther =
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
