namespace Gu.Analyzers.Test.GU0082IdenticalTestCaseTests
{
    using System;
    using System.Collections.Generic;
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Diagnostics
    {
        private static readonly TestMethodAnalyzer Analyzer = new TestMethodAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0082IdenticalTestCase);

        [Test]
        public static void TestCaseAttributeAndParameter()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class C
    {
        [↓TestCase(1, 2)]
        [↓TestCase(1, 2)]
        public void Test(int i, int j)
        {
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void WithAndWithoutAuthor()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class C
    {
        [↓TestCase(1, 2)]
        [↓TestCase(1, 2, Author = ""Author"")]
        public void Test(int i)
        {
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void WithAuthor()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class C
    {
        [↓TestCase(1, 2, Author = ""Author"")]
        [↓TestCase(1, 2, Author = ""Author"")]
        public void Test(int i)
        {
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void Enum()
        {
            var code = @"
namespace N
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void StringAndEnum()
        {
            var code = @"
namespace N
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void Arrays()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class C
    {
        [↓TestCase(new[] { 1, 2 })]
        [↓TestCase(new[] { 1, 2 })]
        public void Test(int[] xs)
        {
        }
    }
}";
            Console.WriteLine(new Dictionary<string, object>().ToString());
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void ArraysWithAndWithoutExplicitTypeSpecification()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class C
    {
        [↓TestCase(new int[] { 1, 2 })]
        [↓TestCase(new[] { 1, 2 })]
        public void Test(int[] xs)
        {
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void ArraysWithAndWithoutAuthor()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class C
    {
        [↓TestCase(new[] { 1, 2 })]
        [↓TestCase(new[] { 1, 2 }, Author = ""Author"")]
        public void Test(int[] xs)
        {
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void ArraysWithAuthor()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class C
    {
        [↓TestCase(new[] { 1, 2 }, Author = ""Author"")]
        [↓TestCase(new[] { 1, 2 }, Author = ""Author"")]
        public void Test(int[] xs)
        {
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void TestCaseParams()
        {
            var code = @"
namespace N
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }
    }
}
