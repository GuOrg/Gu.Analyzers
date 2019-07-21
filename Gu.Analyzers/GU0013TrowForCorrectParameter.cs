namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0013TrowForCorrectParameter
    {
        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: "GU0013",
            title: "Throw for correct parameter.",
            messageFormat: "Throw for correct parameter.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Throw for correct parameter.");
    }
}
