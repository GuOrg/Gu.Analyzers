namespace Gu.Analyzers.Test.GU0082IdenticalTestCaseTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        private static readonly TestMethodAnalyzer Analyzer = new TestMethodAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0082");

        [Test]
        public void TestCaseAttributeAndParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [↓TestCase(1, 2)]
        [↓TestCase(1, 2)]
        public void Test(int i, int j)
        {
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void WithAndWithoutAuthor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [↓TestCase(1, 2)]
        [↓TestCase(1, 2, Author = ""Author"")]
        public void Test(int i)
        {
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void WithAuthor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [↓TestCase(1, 2, Author = ""Author"")]
        [↓TestCase(1, 2, Author = ""Author"")]
        public void Test(int i)
        {
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}