namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0080TestAttributeCountMismatch
    {
        public const string DiagnosticId = "GU0080";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Parameter count does not match attribute.",
            messageFormat: "Parameters {0} does not match attribute {1}.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Parameter count does not match attribute.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}