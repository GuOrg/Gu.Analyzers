namespace Gu.Analyzers
{
    using System.Collections.Immutable;

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
                                                                      id: DiagnosticId,
                                                                      title: Title,
                                                                      messageFormat: MessageFormat,
                                                                      category: AnalyzerCategory.Correctness,
                                                                      defaultSeverity: DiagnosticSeverity.Hidden,
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
            context.RegisterSyntaxNodeAction(HandleArguments, SyntaxKind.ArgumentList);
        }

        private static void HandleArguments(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var argumentListSyntax = (ArgumentListSyntax)context.Node;
            if (argumentListSyntax.Arguments.Count < 4)
            {
                return;
            }

            var method = context.SemanticModel.GetSymbolSafe(argumentListSyntax.Parent, context.CancellationToken) as IMethodSymbol;
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

            if (argumentListSyntax.IsInExpressionTree(context.SemanticModel, context.CancellationToken))
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