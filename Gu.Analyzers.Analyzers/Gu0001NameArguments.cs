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
    internal class GU0001NameArguments : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0001";
        private const string Title = "Name the arguments.";
        private const string MessageFormat = "Name the arguments.";
        private const string Description = "Name the arguments of calls to methods that have more than 3 arguments and are placed on separate lines.";
        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
                                                                      DiagnosticId,
                                                                      Title,
                                                                      MessageFormat,
                                                                      AnalyzerCategory.Correctness,
                                                                      DiagnosticSeverity.Hidden,
                                                                      AnalyzerConstants.EnabledByDefault,
                                                                      Description,
                                                                      HelpLink);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleArguments, SyntaxKind.ArgumentList);
        }

        private static void HandleArguments(SyntaxNodeAnalysisContext context)
        {
            var argumentListSyntax = (ArgumentListSyntax)context.Node;
            if (argumentListSyntax.Arguments.Count < 4)
            {
                return;
            }

            var method = context.SemanticModel.SemanticModelFor(argumentListSyntax.Parent)
                                .GetSymbolInfo(argumentListSyntax.Parent, context.CancellationToken)
                                .Symbol as IMethodSymbol;
            if (method == null ||
                method.ContainingType == KnownSymbol.String ||
                method.ContainingType.Is(KnownSymbol.Tuple) ||
                method.ContainingType == KnownSymbol.DependencyProperty)
            {
                return;
            }

            if (!HasAdjacentParametersOfSameType(method.Parameters))
            {
                return;
            }

            if (IsInExpressionTree(argumentListSyntax, context.SemanticModel, context.CancellationToken))
            {
                return;
            }

            var lineNumber = argumentListSyntax.OpenParenToken.StartingLineNumber(context.CancellationToken);
            foreach (var argument in argumentListSyntax.Arguments)
            {
                var ln = argument.StartingLineNumber(context.CancellationToken);
                if (ln == lineNumber)
                {
                    return;
                }

                lineNumber = ln;
                if (argument.NameColon == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, argumentListSyntax.GetLocation()));
                    return;
                }
            }
        }

        private static bool IsInExpressionTree(ArgumentListSyntax argumentListSyntax, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            foreach (var lamda in argumentListSyntax.Ancestors().OfType<LambdaExpressionSyntax>())
            {
                var lambdaType = semanticModel.GetTypeInfo(lamda, cancellationToken).ConvertedType;
                if (lambdaType != null &&
                    lambdaType.Is(KnownSymbol.Expression))
                {
                    return true;
                }
            }

            return false;
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