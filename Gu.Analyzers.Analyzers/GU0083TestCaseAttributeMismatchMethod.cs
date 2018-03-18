namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal class GU0083TestCaseAttributeMismatchMethod
    {
        public const string DiagnosticId = "GU0083";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "TestCase Arguments Mismatch Method Parameters",
            messageFormat: "TestCase Arguments Mismatch Method Parameters {0}.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "TestCase Mismatches Method Parameters",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
