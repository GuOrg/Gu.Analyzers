namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0004AssignAllReadOnlyMembers
    {
        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: "GU0004",
            title: "Assign all readonly members.",
            messageFormat: "The following readonly members are not assigned:\r\n{0}",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Assign all readonly members.");
    }
}
