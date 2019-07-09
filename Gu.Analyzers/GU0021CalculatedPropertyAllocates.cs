namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0021CalculatedPropertyAllocates
    {
        internal const string DiagnosticId = "GU0021";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Calculated property allocates reference type.",
            messageFormat: "Calculated property allocates reference type.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Calculated property allocates reference type.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
