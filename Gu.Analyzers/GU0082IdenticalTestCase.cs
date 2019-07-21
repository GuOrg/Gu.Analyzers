namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0082IdenticalTestCase
    {
        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: "GU0082",
            title: "TestCase is identical.",
            messageFormat: "TestCase is identical {0}.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "TestCase is identical.");
    }
}
