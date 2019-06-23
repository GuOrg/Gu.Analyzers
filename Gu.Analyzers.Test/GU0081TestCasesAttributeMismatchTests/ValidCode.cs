namespace Gu.Analyzers.Test.GU0081TestCasesAttributeMismatchTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class ValidCode
    {
        private static readonly TestMethodAnalyzer Analyzer = new TestMethodAnalyzer();

        [Test]
        public void TestCaseAttribute()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(1)]
        [TestCase(2)]
        public void Test(int i)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
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
        [TestCase(2, Author = ""Author"")]
        public void Test(int i)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [TestCase("[TestCase(1)]")]
        [TestCase("[TestCase(1, 2)]")]
        [TestCase("[TestCase(1, 2, 3)]")]
        public void TestCaseParams(string testCase)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    class Foo
    {
        [TestCase(1, 2)]
        public void Test(int i, params int[] ints)
        {
        }
    }
}".AssertReplace("[TestCase(1, 2)]", testCase);

            RoslynAssert.Valid(Analyzer, testCode);
        }
    }
}
