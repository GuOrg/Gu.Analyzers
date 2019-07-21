namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0005ExceptionArgumentsPositions
    {
        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: "GU0005",
            title: "Use correct argument positions.",
            messageFormat: "Use correct argument positions.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Use correct position for name and message.");
    }
}
