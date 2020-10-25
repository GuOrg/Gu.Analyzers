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
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.GU0060EnumMemberValueConflictsWithAnother);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(x => Handle(x), SyntaxKind.EnumDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is EnumDeclarationSyntax enumDeclaration)
            {
                if (enumDeclaration.AttributeLists.TryFind(KnownSymbol.FlagsAttribute, context.SemanticModel, context.CancellationToken, out _))
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
                else
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
            }
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
                            when semanticModel.GetSymbolSafe(identifier, cancellationToken) is { ContainingType: { } containingType } &&
                                 containingType.TypeKind != TypeKind.Enum:
                            return false;
                        case LiteralExpressionSyntax _:
                            return false;
                    }
                }

                return true;
            }

            return false;
        }

        // unboxes a boxed integral value to ulong, regardless of the original boxed type
        // negative values are converted to their U2 representation
        // see http://stackoverflow.com/a/10022661/1012936
        private static ulong UnboxUMaxInt(object a)
        {
            return a is ulong ? (ulong)a : (ulong)Convert.ToInt64(a, CultureInfo.InvariantCulture);
        }
    }
}
