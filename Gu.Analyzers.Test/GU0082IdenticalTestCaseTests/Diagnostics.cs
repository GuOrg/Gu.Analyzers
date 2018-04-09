namespace Gu.Analyzers.Test.GU0082IdenticalTestCaseTests
{
    using System;
    using System.Collections.Generic;
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        private static readonly TestMethodAnalyzer Analyzer = new TestMethodAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0082");

        [Test]
        public void TestCaseAttributeAndParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [↓TestCase(1, 2)]
        [↓TestCase(1, 2)]
        public void Test(int i, int j)
        {
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void WithAndWithoutAuthor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [↓TestCase(1, 2)]
        [↓TestCase(1, 2, Author = ""Author"")]
        public void Test(int i)
        {
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void WithAuthor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [↓TestCase(1, 2, Author = ""Author"")]
        [↓TestCase(1, 2, Author = ""Author"")]
        public void Test(int i)
        {
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void Enum()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using NUnit.Framework;

    internal class Foo
    {
        [↓TestCase(StringComparison.Ordinal)]
        [↓TestCase(StringComparison.Ordinal)]
        public void Test(StringComparison stringComparison)
        {
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void StringAndEnum()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using NUnit.Framework;

    internal class Foo
    {
        [↓TestCase(""1"", StringComparison.Ordinal)]
        [↓TestCase(""1"", StringComparison.Ordinal)]
        public void Test(string text, StringComparison stringComparison)
        {
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
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
        [↓TestCase(new[] { 1, 2 })]
        [↓TestCase(new[] { 1, 2 })]
        public void Test(int[] xs)
        {
        }
    }
}";
            Console.WriteLine(new Dictionary<string, object>().ToString());
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void ArraysWithAndWithoutExplicitTypeSpecification()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [↓TestCase(new int[] { 1, 2 })]
        [↓TestCase(new[] { 1, 2 })]
        public void Test(int[] xs)
        {
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void ArraysWithAndWithoutAuthor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [↓TestCase(new[] { 1, 2 })]
        [↓TestCase(new[] { 1, 2 }, Author = ""Author"")]
        public void Test(int[] xs)
        {
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void ArraysWithAuthor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [↓TestCase(new[] { 1, 2 }, Author = ""Author"")]
        [↓TestCase(new[] { 1, 2 }, Author = ""Author"")]
        public void Test(int[] xs)
        {
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
