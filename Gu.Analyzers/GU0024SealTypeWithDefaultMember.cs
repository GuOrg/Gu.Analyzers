namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0024SealTypeWithDefaultMember
    {
        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: "GU0024",
            title: "Seal type with default member.",
            messageFormat: "Seal type with default member.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: false,
            description: "Seal type with default member.");
    }
}
