namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0008AvoidRelayProperties
    {
        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: "GU0008",
            title: "Avoid relay properties.",
            messageFormat: "Avoid relay properties.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.DisabledByDefault,
            description: "Avoid relay properties.");
    }
}
