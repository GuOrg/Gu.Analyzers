namespace Gu.Analyzers.Test.GU0007PreferInjectingTests
{
    using Gu.Analyzers.CodeFixes;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;

    internal partial class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new GU0007PreferInjecting();
        private static readonly CodeFixProvider Fix = new InjectCodeFixProvider();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0007");
    }
}
