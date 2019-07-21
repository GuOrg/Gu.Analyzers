namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0017DonNotUseDiscarded
    {
        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: "GU0017",
            title: "Don't use discarded.",
            messageFormat: "Don't use discarded.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Don't use discarded.");
    }
}
