namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0081TestCasesAttributeMismatch
    {
        internal const string DiagnosticId = "GU0081";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "TestCase does not match parameters.",
            messageFormat: "TestCase {0} does not match parameters {1}.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "TestCase does not match parameters.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
