namespace Gu.Analyzers.Test.GU0080TestAttributeCountMismatchTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        private static readonly TestMethodAnalyzer Analyzer = new TestMethodAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0080");

        [Test]
        public void TestAttributeAndParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [Test]
        public void Test↓(string text)
        {
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void TestAttributeAndParameter_Multiple()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [Test]
        public void Test↓(string text, int index, bool value)
        {
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [TestCase("Test↓()")]
        [TestCase("Test↓(1, 2)")]
        public void TestCaseAttributeAndParameter(string signature)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(1)]
        public void Test↓(int i)
        {
        }
    }
}";
            testCode = testCode.AssertReplace("Test(int i)", signature);
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [TestCase("Test↓()")]
        [TestCase("Test↓(1, 2)")]
        public void TestAndTestCaseAttributeAndParameter(string signature)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(1)]
        public void Test↓(int i)
        {
        }
    }
}";
            testCode = testCode.AssertReplace("Test↓(int i)", signature);
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
