namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0004AssignAllReadOnlyMembers
    {
        internal const string DiagnosticId = "GU0004";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Assign all readonly members.",
            messageFormat: "The following readonly members are not assigned:\r\n{0}",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Assign all readonly members.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
