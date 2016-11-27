namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0006UseNameof : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0006";
        private const string Title = "Use nameof.";
        private const string MessageFormat = "Use nameof.";
        private const string Description = "Use nameof.";
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
            context.RegisterSyntaxNodeAction(HandleArguments, SyntaxKind.Argument);
        }

        private static void HandleArguments(SyntaxNodeAnalysisContext context)
        {
            var argument = (ArgumentSyntax)context.Node;
            var literal = argument.Expression as LiteralExpressionSyntax;
            if (literal?.IsKind(SyntaxKind.StringLiteralExpression) != true)
            {
                return;
            }

            var symbols = context.SemanticModel.LookupSymbols(argument.SpanStart);
            ISymbol symbol;
            if (symbols.TryGetSingle(x => x.Name == literal.Token.ValueText, out symbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, argument.GetLocation()));
            }
        }
    }
}