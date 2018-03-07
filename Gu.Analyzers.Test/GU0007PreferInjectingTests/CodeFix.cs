namespace Gu.Analyzers.Test.GU0007PreferInjectingTests
{
    using Gu.Roslyn.Asserts;

    internal partial class CodeFix
    {
        private static readonly GU0007PreferInjecting Analyzer = new GU0007PreferInjecting();
        private static readonly InjectCodeFixProvider Fix = new InjectCodeFixProvider();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0007");
    }
}