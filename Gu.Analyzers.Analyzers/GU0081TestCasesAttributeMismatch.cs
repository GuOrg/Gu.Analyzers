namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0081TestCasesAttributeMismatch
    {
        public const string DiagnosticId = "GU0081";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "TestCases does not match.",
            messageFormat: "TestCases does not match.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "TestCases does not match.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}