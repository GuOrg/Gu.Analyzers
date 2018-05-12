namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class GU0001NameArguments
    {
        public const string DiagnosticId = "GU0001";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Name the arguments.",
            messageFormat: "Name the arguments.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Name the arguments of calls to methods that have more than 3 arguments and are placed on separate lines.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}