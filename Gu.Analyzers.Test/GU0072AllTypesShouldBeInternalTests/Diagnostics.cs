namespace Gu.Analyzers.Test.GU0072AllTypesShouldBeInternalTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        private static readonly GU0072AllTypesShouldBeInternal Analyzer = new GU0072AllTypesShouldBeInternal();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0072");

        [Explicit("Make it pass")]
        [Test]
        public void PublicClass()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    internal class ↓A
    {
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
