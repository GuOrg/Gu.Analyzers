namespace Gu.Analyzers.Test.GU0081TestCasesAttributeMismatchTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        private static readonly TestMethodAnalyzer Analyzer = new TestMethodAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0081");

        [Test]
        public void TestCaseAttributeAndParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(1)]
        [↓TestCase(1, 2)]
        public void Test(int i)
        {
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void TestCaseAttributeWithAuthor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(1, Author = ""Author"")]
        [↓TestCase(1, 2, Author = ""Author"")]
        public void Test(int i)
        {
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void TestCaseParams()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    class Foo
    {
        [TestCase(1, 2, 3)]
        [↓TestCase(1)]
        public void Test(int i, int j, params int[] ints)
        {
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
