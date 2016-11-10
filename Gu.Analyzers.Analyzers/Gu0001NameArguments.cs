namespace Gu.Analyzers
{
    using System.Collections.Immutable;

    using Gu.Analyzers.Helpers.SyntaxtTreeHelpers;

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
        private static readonly string HelpLink = Gu.Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
                                                                      DiagnosticId,
                                                                      Title,
                                                                      MessageFormat,
                                                                      AnalyzerCategory.Correctness,
                                                                      DiagnosticSeverity.Warning,
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
            if (argumentListSyntax.Arguments.Count < 3)
            {
                return;
            }

            var method = context.SemanticModel.SemanticModelFor(argumentListSyntax.Parent)
                                .GetSymbolInfo(argumentListSyntax.Parent, context.CancellationToken)
                                .Symbol as IMethodSymbol;
            if (method == KnownSymbol.String.Format)
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
    }
}