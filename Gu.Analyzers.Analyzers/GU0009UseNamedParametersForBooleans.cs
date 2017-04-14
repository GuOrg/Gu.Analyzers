namespace Gu.Analyzers
{
    using System.Collections.Immutable;
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
                                                                      defaultSeverity: DiagnosticSeverity.Warning,
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
            if (methodSymbol == null)
            {
                return;
            }

            if (IsIgnored(methodSymbol))
            {
                return;
            }

            if (!ReferenceEquals(methodSymbol.OriginalDefinition, methodSymbol))
            {
                var methodGenericSymbol = methodSymbol.OriginalDefinition;
                var parameterIndexOpt = FindParameterIndexCorrespondingToIndex(methodSymbol, argumentSyntax);
                //// ReSharper disable once IsExpressionAlwaysTrue R# dumbs analysis here.
                if (parameterIndexOpt is int parameterIndex &&
                    methodGenericSymbol.Parameters[parameterIndex]
                                       .Type is ITypeParameterSymbol)
                {
                    return;
                }
            }
            else
            {
                var parameterIndexOpt = FindParameterIndexCorrespondingToIndex(methodSymbol, argumentSyntax);
                if (parameterIndexOpt == null)
                {
                    return;
                }

                var parameterIndex = System.Math.Min(parameterIndexOpt.Value, methodSymbol.Parameters.Length - 1);
                var parameter = methodSymbol.Parameters[parameterIndex];
                if (parameter.IsParams ||
                    parameter.Type != KnownSymbol.Boolean)
                {
                    return;
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(Descriptor, argumentSyntax.GetLocation()));
        }

        private static bool IsLiteralBool(ArgumentSyntax argument)
        {
            var kind = argument.Expression?.Kind();
            return kind == SyntaxKind.TrueLiteralExpression ||
                   kind == SyntaxKind.FalseLiteralExpression;
        }

        private static bool IsIgnored(IMethodSymbol methodSymbol)
        {
            return IsDisposePattern(methodSymbol) ||
                   IsConfigureAwait(methodSymbol) ||
                   IsAttachedSetMethod(methodSymbol) ||
                   methodSymbol == KnownSymbol.NUnitAssert.AreEqual ||
                   methodSymbol == KnownSymbol.XunitAssert.Equal;
        }

        private static bool IsConfigureAwait(IMethodSymbol methodSymbol)
        {
            return (methodSymbol.ReceiverType == KnownSymbol.Task ||
                    methodSymbol.ReceiverType == KnownSymbol.TaskOfT) &&
                   methodSymbol.Name == "ConfigureAwait" &&
                   methodSymbol.Parameters.Length == 1 &&
                   methodSymbol.Parameters[0]
                               .Type == KnownSymbol.Boolean;
        }

        private static bool IsDisposePattern(IMethodSymbol methodSymbol)
        {
            return methodSymbol.Name == "Dispose" &&
                   methodSymbol.Parameters.Length == 1 &&
                   methodSymbol.Parameters[0]
                               .Type == KnownSymbol.Boolean;
        }

        private static bool IsAttachedSetMethod(IMethodSymbol method)
        {
            if (method == null ||
                !method.ReturnsVoid ||
                method.AssociatedSymbol != null)
            {
                return false;
            }

            if (method.IsStatic)
            {
                return method.Parameters.Length == 2 &&
                       method.Parameters[0].Type.Is(KnownSymbol.DependencyObject) &&
                       method.Name.StartsWith("Set");
            }

            return method.IsExtensionMethod &&
                   method.ReceiverType.Is(KnownSymbol.DependencyObject) &&
                   method.Parameters.Length == 1 &&
                   method.Name.StartsWith("Set");

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
    }
}