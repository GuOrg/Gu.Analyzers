namespace Gu.Analyzers.Test.GU0082IdenticalTestCaseTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly TestMethodAnalyzer Analyzer = new TestMethodAnalyzer();

        [Test]
        public void SingleArgument()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void SingleArgumentWithAndWithoutAuthor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(1)]
        [TestCase(2, Author = ""Author"")]
        public void Test(int i)
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void SingleArgumentWithAuthor()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void Arrays()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(new[] { 1, 2 })]
        [TestCase(new[] { 3, 4 })]
        public void Test(int[] xs)
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DoubleAndInt()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(1)]
        [TestCase(1.0)]
        public void Test(object obj)
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}