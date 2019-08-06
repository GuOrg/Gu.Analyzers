namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ArgumentAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptors.GU0009UseNamedParametersForBooleans);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.Argument);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
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
                !IsIgnored(method, context.Compilation))
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

                context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0009UseNamedParametersForBooleans, argumentSyntax.GetLocation()));
            }
        }

        private static bool IsIgnored(IMethodSymbol methodSymbol, Compilation compilation)
        {
            return IsDisposePattern(methodSymbol) ||
                   IsConfigureAwait(methodSymbol) ||
                   IsAttachedSetMethod(methodSymbol, compilation) ||
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

        private static bool IsAttachedSetMethod(IMethodSymbol method, Compilation compilation)
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
                       method.Parameters[0].Type.IsAssignableTo(KnownSymbol.DependencyObject, compilation) &&
                       method.Name.StartsWith("Set");
            }

            return method.IsExtensionMethod &&
                   method.ReceiverType.IsAssignableTo(KnownSymbol.DependencyObject, compilation) &&
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
