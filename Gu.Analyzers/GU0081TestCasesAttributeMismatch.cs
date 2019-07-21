namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0081TestCasesAttributeMismatch
    {
        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: "GU0081",
            title: "TestCase does not match parameters.",
            messageFormat: "TestCase {0} does not match parameters {1}.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "TestCase does not match parameters.");
    }
}
