namespace Gu.Analyzers.Test.GU0083TestCaseAttributeMismatchMethodTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly TestMethodAnalyzer Analyzer = new TestMethodAnalyzer();

        [Test]
        public void TestCaseAttribute_IfParametersAreCorrect_NoAuthor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(1, 2, ""3"")]
        public void Test(int x, int y, string str)
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void TestCaseAttribute_IfParametersAreCorrect_WithAuthor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(1, 2, ""3"", Author=""Author"")]
        public void Test(int x, int y, string str)
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void TestCaseAttribute_IfParametersAreCorrect_AndIsArray()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(int[] {3, 5})]
        public void Test(int[] array)
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void TestCaseAttribute_IfParametersAreCorrect_AndIsArrayMultiple()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(int[] {3, 5}, string[] {""hello""})]
        public void Test(int[] array, string[] stringArray)
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void TestCaseAttribute_IfParametersAreCorrect_AndObjectTypeUsed()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(1)]
        public void Test(object obj)
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}