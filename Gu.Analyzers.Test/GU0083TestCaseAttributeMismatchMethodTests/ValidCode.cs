namespace Gu.Analyzers.Test.GU0083TestCaseAttributeMismatchMethodTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class ValidCode
    {
        private static readonly TestMethodAnalyzer Analyzer = new TestMethodAnalyzer();

        [Test]
        public static void NoAuthor()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("[TestCase(\"a\", \"b\", null)]")]
        [TestCase("[TestCase(null, \"a\", \"b\")]")]
        [TestCase("[TestCase(\"a\", null, \"b\")]")]
        [TestCase("[TestCase(null, null, null)]")]
        public static void NullArgument(string testCase)
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(""x"", ""y"", null)]
        public void Test(string x, string y, string z)
        {
        }
    }
}".AssertReplace("[TestCase(\"x\", \"y\", null)]", testCase);

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ArgumentIsNullAndParameterIsNullableInt()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(null)]
        public void Test(int? obj)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WithAuthor()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void NullArgumentWithAuthor()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(1, 2, null, Author=""Author"")]
        public void Test(int x, int y, string str)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ArrayOfInts()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(new int[] { 3, 5 })]
        public void Test(int[] array)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ArraysOfDifferentTypes()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(new int[] { 3, 5 }, new string[] { ""hello"" })]
        public void Test(int[] array, string[] stringArray)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ParameterOfTypeObject()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ParameterOfInterfaceType()
        {
            var code = @"
namespace N
{
    using System;
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(1)]
        [TestCase(1.0)]
        public void Test(IFormattable obj)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void StringComparison()
        {
            var code = @"
namespace N
{
    using System;
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(StringComparison.CurrentCulture)]
        public void Test(StringComparison stringComparison)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void StringAndStringComparison()
        {
            var code = @"
namespace N
{
    using System;
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(""abc"", StringComparison.CurrentCulture)]
        public void Test(string text, StringComparison stringComparison)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IntToDouble()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(1)]
        public void Test(double d)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ShortToInt()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(short.MinValue)]
        public void Test(int d)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IntToShort()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(1)]
        public void Test(short s)
        {
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

        [TestCase("where T : struct")]
        [TestCase("where T : IComparable")]
        [TestCase("where T : IComparable<T>")]
        [TestCase("where T : struct, IComparable<T>, IComparable")]
        public static void GenericFixtureWithTestCase(string constraints)
        {
            var code = @"
namespace N
{
    using System;
    using NUnit.Framework;

    [TestFixture(typeof(int))]
    [TestFixture(typeof(double))]
    public class Foo<T>
        where T : struct, IComparable<T>, IComparable
    {
        [TestCase(1)]
        public void Test(T value)
        {
        }
    }
}".AssertReplace("where T : struct, IComparable<T>, IComparable", constraints);

            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
