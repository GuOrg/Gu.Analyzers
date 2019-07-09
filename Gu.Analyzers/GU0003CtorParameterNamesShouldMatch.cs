namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0003CtorParameterNamesShouldMatch
    {
        internal const string DiagnosticId = "GU0003";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Name the parameters to match the assigned members.",
            messageFormat: "Name the parameters to match the assigned members.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Name the constructor parameters to match the assigned member.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
