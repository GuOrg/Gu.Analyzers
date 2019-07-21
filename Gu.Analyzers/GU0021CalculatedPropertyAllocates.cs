namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0021CalculatedPropertyAllocates
    {
        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: "GU0021",
            title: "Calculated property allocates reference type.",
            messageFormat: "Calculated property allocates reference type.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Calculated property allocates reference type.");
    }
}
