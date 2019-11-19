namespace Gu.Analyzers
{
    using System;
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
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is ArgumentSyntax { Expression: { } expression, Parent: ArgumentListSyntax { Parent: { } parent } } argument)
            {
                if (ShouldName())
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0009UseNamedParametersForBooleans, argument.GetLocation()));
                }
            }

            bool ShouldName()
            {
                return argument.NameColon is null &&
                       expression.IsEither(SyntaxKind.TrueLiteralExpression, SyntaxKind.FalseLiteralExpression) &&
                       !argument.IsInExpressionTree(context.SemanticModel, context.CancellationToken) &&
                       context.SemanticModel.TryGetSymbol(parent, context.CancellationToken, out IMethodSymbol? method) &&
                       method.FindParameter(argument) is { Type: { SpecialType: SpecialType.System_Boolean } } parameter &&
                       parameter.OriginalDefinition.Type.SpecialType == SpecialType.System_Boolean &&
                       !IsIgnored(method, context.Compilation);
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
                   methodSymbol is { Name: "ConfigureAwait", Parameters: { Length: 1 } parameters } &&
                   parameters[0].Type.SpecialType == SpecialType.System_Boolean;
        }

        private static bool IsDisposePattern(IMethodSymbol methodSymbol)
        {
            return methodSymbol is { Name: "Dispose", Parameters: { Length: 1 } parameters } &&
                   parameters[0].Type.SpecialType == SpecialType.System_Boolean;
        }

        private static bool IsAttachedSetMethod(IMethodSymbol method, Compilation compilation)
        {
            if (method is null ||
                !method.ReturnsVoid ||
                method.AssociatedSymbol != null)
            {
                return false;
            }

            if (method.IsStatic)
            {
                return method.Parameters.Length == 2 &&
                       method.Parameters[0].Type.IsAssignableTo(KnownSymbol.DependencyObject, compilation) &&
                       method.Name.StartsWith("Set", StringComparison.Ordinal);
            }

            return method is { IsExtensionMethod: true } &&
                   method.ReceiverType.IsAssignableTo(KnownSymbol.DependencyObject, compilation) &&
                   method.Parameters.Length == 1 &&
                   method.Name.StartsWith("Set", StringComparison.Ordinal);
        }

        private static int? FindParameterIndexCorrespondingToIndex(IMethodSymbol method, ArgumentSyntax argument)
        {
            if (argument.NameColon is null)
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
