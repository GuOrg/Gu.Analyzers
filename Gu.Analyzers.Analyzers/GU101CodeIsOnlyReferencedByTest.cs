namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU101CodeIsOnlyReferencedByTest : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0101";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Code is only referenced by test.",
            messageFormat: "Code is only referenced by test.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: AnalyzerConstants.DisabledByDefault,
            description: "Code is only used by tests.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationAction(HandleCompilation);
        }

        private static void HandleCompilation(CompilationAnalysisContext context)
        {
            var visitor = new Visitor();
            visitor.Visit(context.Compilation.Assembly);
        }

        private class Visitor : Microsoft.CodeAnalysis.SymbolVisitor
        {
            public override void VisitMethod(IMethodSymbol symbol)
            {
                base.VisitMethod(symbol);
            }
        }
    }
}