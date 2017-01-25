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
        private const string Title = "Use correct argument positions.";
        private const string MessageFormat = "Use correct argument positions.";
        private const string Description = "Use correct position for name and message.";

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
                int parameterIndex;
                ArgumentSyntax argument;
                int argumentIndex;
                if (TryGetIndexOfParameter(ctor, "paramName", out parameterIndex) &&
                    TryGetIndexOfNameArgument(symbols, objectCreationExpressionSyntax.ArgumentList, out argument, out argumentIndex) &&
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
                var literal = argument.Expression as LiteralExpressionSyntax;
                if (literal != null)
                {
                    ISymbol _;
                    if (symbols.TryGetSingle(x => x.Name == literal.Token.ValueText, out _))
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

                    ISymbol _;
                    if (symbols.TryGetSingle(x => x.Name == identifierName.Identifier.ValueText, out _))
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