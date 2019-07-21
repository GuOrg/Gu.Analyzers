namespace Gu.Analyzers.Test.GU0081TestCasesAttributeMismatchTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Diagnostics
    {
        private static readonly TestMethodAnalyzer Analyzer = new TestMethodAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0081TestCasesAttributeMismatch);

        [Test]
        public static void TestCaseAttributeAndParameter()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class C
    {
        [TestCase(1)]
        [↓TestCase(1, 2)]
        public void Test(int i)
        {
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void TestCaseAttributeWithAuthor()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class C
    {
        [TestCase(1, Author = ""Author"")]
        [↓TestCase(1, 2, Author = ""Author"")]
        public void Test(int i)
        {
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void TestCaseParams()
        {
            var code = @"
namespace N
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }
    }
}
