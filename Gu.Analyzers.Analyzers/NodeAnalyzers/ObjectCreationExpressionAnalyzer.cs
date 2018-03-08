namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ObjectCreationExpressionAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(GU0005ExceptionArgumentsPositions.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleObjectCreation, SyntaxKind.ObjectCreationExpression);
        }

        private static void HandleObjectCreation(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is ObjectCreationExpressionSyntax objectCreation &&
                objectCreation.ArgumentList != null &&
                objectCreation.ArgumentList.Arguments.Count > 0 &&
                context.ContainingSymbol is IMethodSymbol method)
            {
                if (objectCreation.TryGetConstructor(KnownSymbol.ArgumentException, context.SemanticModel, context.CancellationToken, out var ctor) ||
                    objectCreation.TryGetConstructor(KnownSymbol.ArgumentNullException, context.SemanticModel, context.CancellationToken, out ctor) ||
                    objectCreation.TryGetConstructor(KnownSymbol.ArgumentOutOfRangeException, context.SemanticModel, context.CancellationToken, out ctor))
                {
                    if (TryGetIndexOfParameter(ctor, "paramName", out var parameterIndex) &&
                        TryGetIndexOfNameArgument(method.Parameters, objectCreation.ArgumentList, out var argument, out var argumentIndex) &&
                        argumentIndex != parameterIndex)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(GU0005ExceptionArgumentsPositions.Descriptor, argument.GetLocation()));
                    }
                }
            }
        }

        private static bool TryGetIndexOfParameter(IMethodSymbol method, string name, out int index)
        {
            if (method == null)
            {
                index = -1;
                return false;
            }

            for (var i = 0; i < method.Parameters.Length; i++)
            {
                if (method.Parameters[i].Name == name)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        private static bool TryGetIndexOfNameArgument(ImmutableArray<IParameterSymbol> parameters, ArgumentListSyntax arguments, out ArgumentSyntax argument, out int index)
        {
            for (var i = 0; i < arguments.Arguments.Count; i++)
            {
                argument = arguments.Arguments[i];
                if (argument.Expression is LiteralExpressionSyntax literal)
                {
                    if (parameters.TrySingle(x => x.Name == literal.Token.ValueText, out ISymbol _))
                    {
                        index = i;
                        return true;
                    }
                }

                if (argument.Expression is InvocationExpressionSyntax invocation &&
                    invocation.Expression is IdentifierNameSyntax methodName &&
                    invocation.ArgumentList != null &&
                    methodName.Identifier.ValueText == "nameof" &&
                    invocation.ArgumentList.Arguments.TryFirst(out var nameofArgument) &&
                    nameofArgument.Expression is IdentifierNameSyntax identifierName &&
                    parameters.TrySingle(x => x.Name == identifierName.Identifier.ValueText, out ISymbol _))
                {
                    index = i;
                    return true;
                }
            }

            argument = null;
            index = -1;
            return false;
        }
    }
}
