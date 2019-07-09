namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0010DoNotAssignSameValue
    {
        internal const string DiagnosticId = "GU0010";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Assigning same value.",
            messageFormat: "Assigning made to same, did you mean to assign something else?",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Assigning same value does not make sense and is sign of a bug.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
