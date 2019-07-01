namespace Gu.Analyzers.Test.GU0082IdenticalTestCaseTests
{
    using System;
    using System.Collections.Generic;
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Diagnostics
    {
        private static readonly TestMethodAnalyzer Analyzer = new TestMethodAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0082IdenticalTestCase.Descriptor);

        [Test]
        public static void TestCaseAttributeAndParameter()
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void WithAndWithoutAuthor()
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void WithAuthor()
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void Enum()
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void StringAndEnum()
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void Arrays()
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void ArraysWithAndWithoutExplicitTypeSpecification()
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void ArraysWithAndWithoutAuthor()
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void ArraysWithAuthor()
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void TestCaseParams()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    class Foo
    {
        [↓TestCase(1, 2, 3)]
        [↓TestCase(1, 2, 3)]
        [TestCase(1)]
        public void Test(int i, int j, params int[] ints)
        {
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
