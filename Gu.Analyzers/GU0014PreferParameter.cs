namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0014PreferParameter
    {
        internal const string DiagnosticId = "GU0014";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Prefer using parameter.",
            messageFormat: "Prefer using parameter.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Prefer using parameter.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
