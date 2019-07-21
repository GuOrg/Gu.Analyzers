namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0012NullCheckParameter
    {
        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: "GU0012",
            title: "Check if parameter is null.",
            messageFormat: "Check if parameter is null.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Check if parameter is null.");
    }
}
