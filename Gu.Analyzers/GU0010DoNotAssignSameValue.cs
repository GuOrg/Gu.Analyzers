namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0010DoNotAssignSameValue
    {
        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: "GU0010",
            title: "Assigning same value.",
            messageFormat: "Assigning made to same, did you mean to assign something else?",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Assigning same value does not make sense and is sign of a bug.");
    }
}
