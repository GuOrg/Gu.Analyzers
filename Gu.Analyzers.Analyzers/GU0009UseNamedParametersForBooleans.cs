namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0009UseNamedParametersForBooleans : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0009";
        private const string Title = "Name the boolean parameter.";
        private const string MessageFormat = "The boolean parameter is not named.";
        private const string Description = "The unnamed boolean parameters aren't obvious about their purpose. Consider naming the boolean argument for clarity.";
        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
                                                                      id: DiagnosticId,
                                                                      title: Title,
                                                                      messageFormat: MessageFormat,
                                                                      category: AnalyzerCategory.Correctness,
                                                                      defaultSeverity: DiagnosticSeverity.Hidden,
                                                                      isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
                                                                      description: Description,
                                                                      helpLinkUri: HelpLink);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleArgument, SyntaxKind.Argument);
        }

        private static bool IsLiteralBool(ArgumentSyntax argument)
        {
            var kind = argument.Expression.Kind();
            return kind == SyntaxKind.TrueLiteralExpression ||
                   kind == SyntaxKind.FalseLiteralExpression;
        }

        private static int? FindParameterIndexCorrespondingToIndex(IMethodSymbol method, ArgumentSyntax argument)
        {
            if (argument.NameColon == null)
            {
                var index = argument.FirstAncestorOrSelf<ArgumentListSyntax>()
                                    .Arguments.IndexOf(argument);
                return index;
            }

            for (int i = 0; i < method.Parameters.Length; ++i)
            {
                var candidate = method.Parameters[i];
                if (candidate.Name == argument.NameColon.Name.Identifier.ValueText)
                {
                    return i;
                }
            }

            return null;
        }

        private static void HandleArgument(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var argumentSyntax = (ArgumentSyntax)context.Node;
            if (!IsLiteralBool(argumentSyntax))
            {
                return;
            }

            if (argumentSyntax.NameColon != null)
            {
                return;
            }

            var methodSymbol = context.SemanticModel.GetSymbolSafe(argumentSyntax.FirstAncestor<ArgumentListSyntax>().Parent, context.CancellationToken) as IMethodSymbol;
            if (methodSymbol != null && !ReferenceEquals(methodSymbol.OriginalDefinition, methodSymbol))
            {
                var methodGenericSymbol = methodSymbol.OriginalDefinition;
                var parameterIndexOpt = FindParameterIndexCorrespondingToIndex(methodSymbol, argumentSyntax);
                if (parameterIndexOpt is int parameterIndex &&
                    methodGenericSymbol.Parameters[parameterIndex]
                                       .Type is ITypeParameterSymbol)
                {
                    return;
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(Descriptor, argumentSyntax.GetLocation()));
        }
    }
}