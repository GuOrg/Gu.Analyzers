namespace Gu.Analyzers.Test.GU0080TestAttributeCountMismatchTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using NUnit.Framework;

    internal class CodeFix
    {
        private static readonly TestMethodAnalyzer Analyzer = new TestMethodAnalyzer();
        private static readonly CodeFixProvider Fix = new TestMethodParametersFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0080TestAttributeCountMismatch.Descriptor);

        [TestCase("string text")]
        [TestCase("string text, int index, bool value")]
        public void TestAttributeAndParameter(string parameters)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [Test]
        public void Test↓(string text)
        {
        }
    }
}".AssertReplace("string text", parameters);

            var fixedCode = @"
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
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

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
        public void Test↓()
        {
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(1)]
        public void Test(int arg0)
        {
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [TestCase("int i, int j")]
        [TestCase("string i, int j")]
        [TestCase("string i, int j, int k")]
        [TestCase("string i,string j, int k, int l")]
        public void TestCaseAttributeAndTooManyParameters(string parameters)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(1)]
        public void Test↓(int i, int j)
        {
        }
    }
}".AssertReplace("int i, int j", parameters);

            var fixedCode = @"
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
}";

            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
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
        [TestCase(1)]
        public void Test↓(int i, int j, params int[] ints)
        {
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
