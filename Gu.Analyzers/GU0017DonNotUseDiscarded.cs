namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0017DonNotUseDiscarded
    {
        public const string DiagnosticId = "GU0017";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Don't use discarded.",
            messageFormat: "Don't use discarded.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Don't use discarded.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}