namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0012NullCheckParameter
    {
        public const string DiagnosticId = "GU0012";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Check if parameter is null.",
            messageFormat: "Check if parameter is null.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Check if parameter is null.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}