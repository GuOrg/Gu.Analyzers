namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0024SealTypeWithDefaultMember
    {
        public const string DiagnosticId = "GU0024";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Seal type with default member.",
            messageFormat: "Seal type with default member.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: false,
            description: "Seal type with default member.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}