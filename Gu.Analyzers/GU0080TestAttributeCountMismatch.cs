namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0080TestAttributeCountMismatch
    {
        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: "GU0080",
            title: "Parameter count does not match attribute.",
            messageFormat: "Parameters {0} does not match attribute {1}.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Parameter count does not match attribute.");
    }
}
