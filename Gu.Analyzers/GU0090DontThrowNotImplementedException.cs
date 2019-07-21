namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0090DontThrowNotImplementedException
    {
        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: "GU0090",
            title: "Don't throw NotImplementedException.",
            messageFormat: "Don't throw NotImplementedException.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Don't throw NotImplementedException.");
    }
}
