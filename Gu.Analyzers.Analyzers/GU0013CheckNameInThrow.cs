namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0013CheckNameInThrow
    {
        public const string DiagnosticId = "GU0013";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Use correct parameter name.",
            messageFormat: "Use correct parameter name.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Use correct parameter name.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}