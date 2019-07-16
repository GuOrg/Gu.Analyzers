namespace Gu.Analyzers.Test.GU0082IdenticalTestCaseTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class ValidCode
    {
        private static readonly TestMethodAnalyzer Analyzer = new TestMethodAnalyzer();

        [Test]
        public static void SingleArgument()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void SingleArgumentWithAndWithoutAuthor()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void SingleArgumentWithAuthor()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void Arrays()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DoubleAndInt()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DoubleAndIntMaxValue()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(int.MaxValue)]
        [TestCase(double.MaxValue)]
        public void WithDouble(double value)
        {
            Assert.AreEqual(value, 1 * value);
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DoubleMaxValueAndMinValue()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(double.MinValue)]
        [TestCase(double.MaxValue)]
        public void WithDouble(double value)
        {
            Assert.AreEqual(value, 1 * value);
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
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
        [TestCase(1, 2)]
        [TestCase(1, 2, 3)]
        public void Test(int i, params int[] ints)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void TwoEnumsSameTypes()
        {
            var code = @"
namespace N
{
    using System;
    using NUnit.Framework;

    class C
    {
        [TestCase(StringComparison.Ordinal, StringComparison.CurrentCulture)]
        [TestCase(StringComparison.Ordinal, StringComparison.InvariantCulture)]
        public void M(StringComparison comparison, StringComparison modifiers)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void TwoEnumsDifferentTypes()
        {
            var code = @"
namespace N
{
    using System;
    using NUnit.Framework;

    class C
    {
        [TestCase(StringComparison.Ordinal, ConsoleModifiers.Alt)]
        [TestCase(StringComparison.Ordinal, ConsoleModifiers.Shift)]
        public void M(StringComparison comparison, ConsoleModifiers modifiers)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
