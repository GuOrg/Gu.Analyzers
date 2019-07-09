namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal class GU0083TestCaseAttributeMismatchMethod
    {
        internal const string DiagnosticId = "GU0083";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "TestCase Arguments Mismatch Method Parameters",
            messageFormat: "TestCase arguments {0} does not match method parameters {1}.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "TestCase Mismatches Method Parameters",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
