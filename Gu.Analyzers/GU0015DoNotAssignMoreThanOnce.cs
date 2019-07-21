namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0015DoNotAssignMoreThanOnce
    {
        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: "GU0015",
            title: "Don't assign same more than once.",
            messageFormat: "Don't assign same more than once.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: "Don't assign same more than once.");
    }
}
