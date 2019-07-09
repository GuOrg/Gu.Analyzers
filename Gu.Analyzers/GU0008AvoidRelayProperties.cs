namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0008AvoidRelayProperties
    {
        internal const string DiagnosticId = "GU0008";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Avoid relay properties.",
            messageFormat: "Avoid relay properties.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.DisabledByDefault,
            description: "Avoid relay properties.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
