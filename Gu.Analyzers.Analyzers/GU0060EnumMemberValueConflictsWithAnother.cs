namespace Gu.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0060EnumMemberValueConflictsWithAnother : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0060";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Enum member value conflict.",
            messageFormat: "Enum member value conflicts with another.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "The enum member has a value shared with the other enum member, but it's not explicitly declared as its alias. To fix this, assign a enum member",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(this.HandleEnumMember, SyntaxKind.EnumDeclaration);
        }

        private static bool IsDerivedFromOtherEnumMembers(EnumMemberDeclarationSyntax enumMember, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (enumMember.EqualsValue == null)
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

            if (hasFlagsAttribute)
            {
                this.HandleFlagEnumMember(context, enumDeclaration);
            }
            else
            {
                this.HandleNonFlagEnumMember(context, enumDeclaration);
            }
        }

        private void HandleNonFlagEnumMember(SyntaxNodeAnalysisContext context, EnumDeclarationSyntax enumDeclaration)
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
                        context.ReportDiagnostic(Diagnostic.Create(Descriptor, enumMember.GetLocation()));
                    }

                    enumValuesSet.Add(value).IgnoreReturnValue();
                }
            }
        }

        private void HandleFlagEnumMember(SyntaxNodeAnalysisContext context, EnumDeclarationSyntax enumDeclaration)
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
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, enumMember.GetLocation()));
                }

                bitSumOfLiterals |= value;
            }
        }
    }
}
