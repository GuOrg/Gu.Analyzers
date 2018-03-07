namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0005ExceptionArgumentsPositions : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0005";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Use correct argument positions.",
            messageFormat: "Use correct argument positions.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Use correct position for name and message.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptor);

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
                objectCreation.ArgumentList.Arguments.Count > 0)
            {
                if (objectCreation.TryGetConstructor(KnownSymbol.ArgumentException, context.SemanticModel, context.CancellationToken, out var ctor) ||
                    objectCreation.TryGetConstructor(KnownSymbol.ArgumentNullException, context.SemanticModel, context.CancellationToken, out ctor) ||
                    objectCreation.TryGetConstructor(KnownSymbol.ArgumentOutOfRangeException, context.SemanticModel, context.CancellationToken, out ctor))
                {
                    var symbols = context.SemanticModel.LookupSymbols(objectCreation.SpanStart);
                    if (TryGetIndexOfParameter(ctor, "paramName", out var parameterIndex) &&
                        TryGetIndexOfNameArgument(symbols, objectCreation.ArgumentList, out var argument, out var argumentIndex) &&
                        argumentIndex != parameterIndex)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptor, argument.GetLocation()));
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

        private static bool TryGetIndexOfNameArgument(ImmutableArray<ISymbol> symbols, ArgumentListSyntax arguments, out ArgumentSyntax argument, out int index)
        {
            for (var i = 0; i < arguments.Arguments.Count; i++)
            {
                argument = arguments.Arguments[i];
                if (argument.Expression is LiteralExpressionSyntax literal)
                {
                    if (symbols.TrySingle(x => x.Name == literal.Token.ValueText, out ISymbol _))
                    {
                        index = i;
                        return true;
                    }
                }

                var invocationExpression = argument.Expression as InvocationExpressionSyntax;
                if ((invocationExpression?.Expression as IdentifierNameSyntax)?.Identifier.ValueText == "nameof")
                {
                    var identifierName = invocationExpression.ArgumentList.Arguments[0].Expression as IdentifierNameSyntax;
                    if (identifierName == null)
                    {
                        continue;
                    }

                    if (symbols.TrySingle(x => x.Name == identifierName.Identifier.ValueText, out ISymbol _))
                    {
                        index = i;
                        return true;
                    }
                }
            }

            argument = null;
            index = -1;
            return false;
        }
    }
}