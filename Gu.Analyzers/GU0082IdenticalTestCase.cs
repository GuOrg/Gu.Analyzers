namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0082IdenticalTestCase
    {
        public const string DiagnosticId = "GU0082";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "TestCase is identical.",
            messageFormat: "TestCase is identical {0}.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "TestCase is identical.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}