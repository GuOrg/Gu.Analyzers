namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0016PreferLambda
    {
        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: "GU0016",
            title: "Prefer lambda.",
            messageFormat: "Prefer lambda to reduce allocations.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: false,
            description: "Prefer lambda to reduce allocations.");
    }
}
