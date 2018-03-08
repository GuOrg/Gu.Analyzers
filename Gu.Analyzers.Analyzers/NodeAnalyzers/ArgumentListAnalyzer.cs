namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ArgumentListAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            GU0001NameArguments.Descriptor,
            GU0002NamedArgumentPositionMatches.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleArgumentList, SyntaxKind.ArgumentList);
        }

        private static void HandleArgumentList(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is ArgumentListSyntax argumentList &&
                argumentList.Arguments.Count > 0)
            {
                if (ShouldNameArguments(context, argumentList))
                {
                    context.ReportDiagnostic(Diagnostic.Create(GU0001NameArguments.Descriptor, argumentList.GetLocation()));
                }

                if (!NamedArgumentPositionMatches(context, argumentList))
                {
                    context.ReportDiagnostic(Diagnostic.Create(GU0002NamedArgumentPositionMatches.Descriptor, argumentList.GetLocation()));
                }
            }
        }

        private static bool ShouldNameArguments(SyntaxNodeAnalysisContext context, ArgumentListSyntax argumentListSyntax)
        {
            if (argumentListSyntax.Arguments.Count < 4)
            {
                return false;
            }

            if (argumentListSyntax.IsInExpressionTree(context.SemanticModel, context.CancellationToken))
            {
                return false;
            }

            if (context.SemanticModel.GetSymbolSafe(argumentListSyntax.Parent, context.CancellationToken) is IMethodSymbol method)
            {
                if (method.ContainingType == KnownSymbol.String ||
                    method.ContainingType.Is(KnownSymbol.Tuple) ||
                    method.ContainingType == KnownSymbol.DependencyProperty)
                {
                    return false;
                }

                if (!HasAdjacentParametersOfSameType(method.Parameters))
                {
                    return false;
                }

                var lineNumber = argumentListSyntax.OpenParenToken.StartingLineNumber(context.CancellationToken);
                foreach (var argument in argumentListSyntax.Arguments)
                {
                    var ln = argument.StartingLineNumber(context.CancellationToken);
                    if (ln == lineNumber)
                    {
                        return false;
                    }

                    lineNumber = ln;
                    if (argument.NameColon == null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool NamedArgumentPositionMatches(SyntaxNodeAnalysisContext context, ArgumentListSyntax argumentList)
        {
            if (!argumentList.Arguments.TryFirst(x => x.NameColon != null, out _))
            {
                return true;
            }

            if (context.SemanticModel.GetSymbolSafe(argumentList.Parent, context.CancellationToken) is IMethodSymbol method)
            {
                if (method.Parameters.Length != argumentList.Arguments.Count)
                {
                    return true;
                }

                for (var i = 0; i < argumentList.Arguments.Count; i++)
                {
                    var argument = argumentList.Arguments[i];
                    var parameter = method.Parameters[i];
                    if (argument.NameColon?.Name is IdentifierNameSyntax nameColon &&
                        parameter.Name != nameColon.Identifier.ValueText)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool HasAdjacentParametersOfSameType(ImmutableArray<IParameterSymbol> parameters)
        {
            IParameterSymbol previous = null;
            foreach (var parameter in parameters)
            {
                if (previous != null)
                {
                    if (parameter.Type.Name == previous.Type.Name)
                    {
                        return true;
                    }
                }

                previous = parameter;
            }

            return false;
        }
    }
}
