namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0015DoNotAssignMoreThanOnce
    {
        public const string DiagnosticId = "GU0015";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Don't assign same more than once.",
            messageFormat: "Don't assign same more than once.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: "Don't assign same more than once.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
