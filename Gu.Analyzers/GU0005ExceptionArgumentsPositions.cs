namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0005ExceptionArgumentsPositions
    {
        internal const string DiagnosticId = "GU0005";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Use correct argument positions.",
            messageFormat: "Use correct argument positions.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Use correct position for name and message.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
