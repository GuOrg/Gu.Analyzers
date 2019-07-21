namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal class GU0083TestCaseAttributeMismatchMethod
    {
        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: "GU0083",
            title: "TestCase Arguments Mismatch Method Parameters",
            messageFormat: "TestCase arguments {0} does not match method parameters {1}.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "TestCase Mismatches Method Parameters");
    }
}
