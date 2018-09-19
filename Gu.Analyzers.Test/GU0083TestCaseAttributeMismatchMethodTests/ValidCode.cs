namespace Gu.Analyzers.Test.GU0083TestCaseAttributeMismatchMethodTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class ValidCode
    {
        private static readonly TestMethodAnalyzer Analyzer = new TestMethodAnalyzer();

        [Test]
        public void NoAuthor()
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

        [TestCase("[TestCase(\"a\", \"b\", null)]")]
        [TestCase("[TestCase(null, \"a\", \"b\")]")]
        [TestCase("[TestCase(\"a\", null, \"b\")]")]
        [TestCase("[TestCase(null, null, null)]")]
        public void NullArgument(string testCase)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using NUnit.Framework;

    public class FooTests
    {
        [TestCase(""x"", ""y"", null)]
        public void Test(string x, string y, string z)
        {
        }
    }
}";
            testCode = testCode.AssertReplace("[TestCase(\"x\", \"y\", null)]", testCase);
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ArgumentIsNullAndParameterIsNullableInt()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
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
        [TestCase(1, 2, ""3"", Author=""Author"")]
        public void Test(int x, int y, string str)
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void NullArgumentWithAuthor()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ArrayOfInts()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ArraysOfDifferentTypes()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ParameterOfTypeObject()
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

        [Test]
        public void ParameterOfInterfaceType()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void StringComparison()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void StringAndStringComparison()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IntToDouble()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ShortToInt()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IntToShort()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
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
        [TestCase(1, 2)]
        [TestCase(1, 2, 3)]
        public void Test(int i, params int[] ints)
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("where T : struct")]
        [TestCase("where T : IComparable")]
        [TestCase("where T : IComparable<T>")]
        [TestCase("where T : struct, IComparable<T>, IComparable")]
        public void GenericFixtureWithTestCase(string constraints)
        {
            var testCode = @"
namespace RoslynSandbox
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
}";
            testCode = testCode.AssertReplace("where T : struct, IComparable<T>, IComparable", constraints);
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
