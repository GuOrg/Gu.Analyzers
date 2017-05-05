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

        private static bool IsDerivedFromOtherEnumMembers(EnumMemberDeclarationSyntax enumMember, SemanticModel sema, CancellationToken cancellationToken)
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
                    bool isEnumMember = sema.GetSymbolSafe(identifier, cancellationToken)
                                            .TryGetSingleDeclaration(cancellationToken, out EnumMemberDeclarationSyntax _);
                    return isEnumMember;
                }
            }

            return true;
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