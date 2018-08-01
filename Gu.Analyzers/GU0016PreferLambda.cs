namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0016PreferLambda
    {
        public const string DiagnosticId = "GU0016";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Prefer lambda.",
            messageFormat: "Prefer lambda to reduce allocations.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: false,
            description: "Prefer lambda to reduce allocations.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
