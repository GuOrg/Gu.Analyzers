namespace Gu.Analyzers.Test.GU0081TestCasesAttributeMismatchTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Valid
    {
        private static readonly TestMethodAnalyzer Analyzer = new TestMethodAnalyzer();

        [Test]
        public static void TestCaseAttribute()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class C
    {
        [TestCase(1)]
        [TestCase(2)]
        public void M(int i)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
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
        [TestCase(2, Author = ""Author"")]
        public void M(int i)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("[TestCase(1)]")]
        [TestCase("[TestCase(1, 2)]")]
        [TestCase("[TestCase(1, 2, 3)]")]
        public static void TestCaseParams(string testCase)
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    class C
    {
        [TestCase(1, 2)]
        public void M(int i, params int[] ints)
        {
        }
    }
}".AssertReplace("[TestCase(1, 2)]", testCase);

            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
