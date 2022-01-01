namespace Gu.Analyzers.Test.GU0080TestAttributeCountMismatchTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class Valid
{
    private static readonly TestMethodAnalyzer Analyzer = new();

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

    public class C
    {
        [Test]
        public void M()
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

    public class C
    {
        [TestCase(1)]
        public void M(int i)
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

    public class C
    {
        [Test]
        [TestCase(1)]
        public void M(int i)
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

    public class C
    {
        private static readonly int[] TestCases = { 1, 2, 3 };

        [TestCaseSource(nameof(TestCases))]
        public void M(int value)
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

    class C
    {
        [Test]
        [TestCase(1, 2, 3)]
        public void M(int i, params int[] ints)
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

    class C
    {
        [TestCase(1, 2, 3)]
        public void M(int i, params int[] ints)
        {
        }
    }
}".AssertReplace("[TestCase(1, 2, 3)]", testCase);

        RoslynAssert.Valid(Analyzer, code);
    }
}