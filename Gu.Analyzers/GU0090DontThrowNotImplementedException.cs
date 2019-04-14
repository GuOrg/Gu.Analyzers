namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0090DontThrowNotImplementedException
    {
        public const string DiagnosticId = "GU0090";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Don't throw NotImplementedException.",
            messageFormat: "Don't throw NotImplementedException.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Don't throw NotImplementedException.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
