namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0013TrowForCorrectParameter
    {
        internal const string DiagnosticId = "GU0013";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Throw for correct parameter.",
            messageFormat: "Throw for correct parameter.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Throw for correct parameter.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
