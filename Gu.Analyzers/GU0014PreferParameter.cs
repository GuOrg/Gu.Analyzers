namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0014PreferParameter
    {
        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: "GU0014",
            title: "Prefer using parameter.",
            messageFormat: "Prefer using parameter.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Prefer using parameter.");
    }
}
