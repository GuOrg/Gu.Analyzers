namespace Gu.Analyzers.Test.GU0080TestAttributeCountMismatchTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class ValidCode
    {
        private static readonly TestMethodAnalyzer Analyzer = new TestMethodAnalyzer();

        [TestCase("[Test]")]
        [TestCase("[Test(Author = \"Author\")]")]
        [TestCase("[TestAttribute]")]
        [TestCase("[TestAttribute()]")]
        public static void TestAttribute(string attribute)
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class FooTests
    {
        [Test]
        public void Test()
        {
        }
    }
}".AssertReplace("[Test]", attribute);

            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("[TestCase(1)]")]
        [TestCase("[TestCase(1, Author = \"Author\")]")]
        public static void TestCaseAttribute(string attribute)
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(1)]
        public void Test(int i)
        {
        }
    }
}".AssertReplace("[TestCase(1)]", attribute);

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void TestAndTestCaseAttribute()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class FooTests
    {
        [Test]
        [TestCase(1)]
        public void Test(int i)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void TestCaseSourceAttribute()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class FooTests
    {
        private static readonly int[] TestCases = { 1, 2, 3 };

        [TestCaseSource(nameof(TestCases))]
        public void Test(int value)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("[TestCase(1)]")]
        [TestCase("[TestCase(1, 2)]")]
        [TestCase("[TestCase(1, 2, 3)]")]
        public static void TestAndTestCaseParams(string testCase)
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    class Foo
    {
        [Test]
        [TestCase(1, 2, 3)]
        public void Test(int i, params int[] ints)
        {
        }
    }
}".AssertReplace("[TestCase(1, 2, 3)]", testCase);

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

    class Foo
    {
        [TestCase(1, 2, 3)]
        public void Test(int i, params int[] ints)
        {
        }
    }
}".AssertReplace("[TestCase(1, 2, 3)]", testCase);

            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
