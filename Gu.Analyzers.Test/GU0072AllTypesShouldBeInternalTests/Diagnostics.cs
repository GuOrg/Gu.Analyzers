namespace Gu.Analyzers.Test.GU0072AllTypesShouldBeInternalTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        private static readonly GU0072AllTypesShouldBeInternal Analyzer = new GU0072AllTypesShouldBeInternal();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0072");

        [Test]
        public void AllTypesInternal_PublicClass()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    public class ↓A
    {
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
