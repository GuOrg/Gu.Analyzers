namespace Gu.Analyzers.Test.GU0007PreferInjectingTests;

using Gu.Roslyn.Asserts;

internal static partial class CodeFix
{
    private static readonly DefaultEnabledAnalyzer Analyzer = new(new GU0007PreferInjecting());
    private static readonly InjectFix Fix = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0007PreferInjecting);
}
