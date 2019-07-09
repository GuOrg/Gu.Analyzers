namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0002NamedArgumentPositionMatches
    {
        internal const string DiagnosticId = "GU0002";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "The position of a named argument should match.",
            messageFormat: "The position of a named arguments and parameters should match.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "The position of a named argument should match.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
