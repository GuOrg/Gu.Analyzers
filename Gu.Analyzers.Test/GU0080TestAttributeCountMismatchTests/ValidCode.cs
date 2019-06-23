namespace Gu.Analyzers.Test.GU0080TestAttributeCountMismatchTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class ValidCode
    {
        private static readonly TestMethodAnalyzer Analyzer = new TestMethodAnalyzer();

        [TestCase("[Test]")]
        [TestCase("[Test(Author = \"Author\")]")]
        [TestCase("[TestAttribute]")]
        [TestCase("[TestAttribute()]")]
        public void TestAttribute(string attribute)
        {
            var testCode = @"
namespace RoslynSandbox
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

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [TestCase("[TestCase(1)]")]
        [TestCase("[TestCase(1, Author = \"Author\")]")]
        public void TestCaseAttribute(string attribute)
        {
            var testCode = @"
namespace RoslynSandbox
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

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void TestAndTestCaseAttribute()
        {
            var testCode = @"
namespace RoslynSandbox
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void TestCaseSourceAttribute()
        {
            var testCode = @"
namespace RoslynSandbox
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [TestCase("[TestCase(1)]")]
        [TestCase("[TestCase(1, 2)]")]
        [TestCase("[TestCase(1, 2, 3)]")]
        public void TestAndTestCaseParams(string testCase)
        {
            var testCode = @"
namespace RoslynSandbox
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
        [TestCase(1, 2, 3)]
        public void Test(int i, params int[] ints)
        {
        }
    }
}".AssertReplace("[TestCase(1, 2, 3)]", testCase);

            RoslynAssert.Valid(Analyzer, testCode);
        }
    }
}
