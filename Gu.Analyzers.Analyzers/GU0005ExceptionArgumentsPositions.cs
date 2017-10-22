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

            var objectCreationExpressionSyntax = (ObjectCreationExpressionSyntax)context.Node;
            if (objectCreationExpressionSyntax.IsMissing ||
                objectCreationExpressionSyntax.ArgumentList == null ||
                objectCreationExpressionSyntax.ArgumentList.Arguments.Count < 2)
            {
                return;
            }

            var type = context.SemanticModel.GetTypeInfo(objectCreationExpressionSyntax, context.CancellationToken).Type;
            if (type == KnownSymbol.ArgumentException ||
                type == KnownSymbol.ArgumentNullException ||
                type == KnownSymbol.ArgumentOutOfRangeException)
            {
                var symbols = context.SemanticModel.LookupSymbols(objectCreationExpressionSyntax.SpanStart);
                var ctor = (IMethodSymbol)context.SemanticModel.GetSymbolSafe(objectCreationExpressionSyntax, context.CancellationToken);
                if (TryGetIndexOfParameter(ctor, "paramName", out int parameterIndex) &&
                    TryGetIndexOfNameArgument(symbols, objectCreationExpressionSyntax.ArgumentList, out ArgumentSyntax argument, out int argumentIndex) &&
                    argumentIndex != parameterIndex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, argument.GetLocation()));
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
                    if (symbols.TryGetSingle(x => x.Name == literal.Token.ValueText, out ISymbol _))
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

                    if (symbols.TryGetSingle(x => x.Name == identifierName.Identifier.ValueText, out ISymbol _))
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