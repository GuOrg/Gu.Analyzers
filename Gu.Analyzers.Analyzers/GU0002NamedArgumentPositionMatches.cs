namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0002NamedArgumentPositionMatches : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0002";
        private const string Title = "The position of a named argument should match.";
        private const string MessageFormat = "Use correct positions.";
        private const string Description = "The position of a named argument should match.";
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
            var argumentSyntax = (ArgumentListSyntax)context.Node;
            if (argumentSyntax.Arguments.Any(x => x.NameColon == null))
            {
                return;
            }

            var argumentListSyntax = argumentSyntax.FirstAncestorOrSelf<ArgumentListSyntax>();
            if (argumentListSyntax == null)
            {
                return;
            }

            var method = context.SemanticModel.SemanticModelFor(argumentListSyntax.Parent)
                                .GetSymbolInfo(argumentListSyntax.Parent, context.CancellationToken)
                                .Symbol as IMethodSymbol;
            if (method == null || method.Parameters.Length != argumentListSyntax.Arguments.Count)
            {
                return;
            }

            for (var i = 0; i < argumentListSyntax.Arguments.Count; i++)
            {
                var argument = argumentListSyntax.Arguments[i];
                var parameter = method.Parameters[i];
                if (parameter.Name != argument.NameColon.Name.Identifier.ValueText)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, argumentListSyntax.GetLocation()));
                    return;
                }
            }
        }
    }
}