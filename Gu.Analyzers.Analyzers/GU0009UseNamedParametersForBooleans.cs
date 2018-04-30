namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0009UseNamedParametersForBooleans : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0009";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Name the boolean parameter.",
            messageFormat: "The boolean parameter is not named.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "The unnamed boolean parameters aren't obvious about their purpose. Consider naming the boolean argument for clarity.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

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

            if (context.Node is ArgumentSyntax argumentSyntax &&
                argumentSyntax.NameColon == null &&
                argumentSyntax.Expression is ExpressionSyntax expression &&
                expression.IsEither(SyntaxKind.TrueLiteralExpression, SyntaxKind.FalseLiteralExpression) &&
                !argumentSyntax.IsInExpressionTree(context.SemanticModel, context.CancellationToken) &&
                argumentSyntax.Parent is ArgumentListSyntax argumentList &&
                context.SemanticModel.TryGetSymbol(argumentList.Parent, context.CancellationToken, out IMethodSymbol method) &&
                !IsIgnored(method))
            {
                if (!ReferenceEquals(method.OriginalDefinition, method))
                {
                    var methodGenericSymbol = method.OriginalDefinition;
                    var parameterIndexOpt = FindParameterIndexCorrespondingToIndex(method, argumentSyntax);
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
                    var parameterIndexOpt = FindParameterIndexCorrespondingToIndex(method, argumentSyntax);
                    if (parameterIndexOpt == null)
                    {
                        return;
                    }

                    var parameterIndex = System.Math.Min(parameterIndexOpt.Value, method.Parameters.Length - 1);
                    var parameter = method.Parameters[parameterIndex];
                    if (parameter.IsParams ||
                        parameter.Type != KnownSymbol.Boolean)
                    {
                        return;
                    }
                }

                context.ReportDiagnostic(Diagnostic.Create(Descriptor, argumentSyntax.GetLocation()));
            }
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
