namespace Gu.Analyzers.Test.GU0002NamedArgumentPositionMatchesTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class ValidCode
    {
        private static readonly ArgumentListAnalyzer Analyzer = new ArgumentListAnalyzer();

        [TestCase("new Foo(a, b)")]
        [TestCase("new Foo(a: a, b: b)")]
        [TestCase("new Foo(a, b: b)")]
        public void ConstructorCallWithTwoArguments(string call)
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a, int b)
        {
            this.A = a;
            this.B = b;
        }

        public int A { get; }

        public int B { get; }

        private Foo Create(int a, int b)
        {
            return new Foo(a, b);
        }
    }
}";
            testCode = testCode.AssertReplace("new Foo(a, b)", call);
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("new Foo(a, b)")]
        [TestCase("new Foo(a: a, b: b)")]
        [TestCase("new Foo(a, b: b)")]
        public void ConstructorCallWithTwoArgumentsStruct(string call)
        {
            var testCode = @"
namespace RoslynSandbox
{
    public struct Foo
    {
        public Foo(int a, int b)
        {
            this.A = a;
            this.B = b;
        }

        public int A { get; }

        public int B { get; }

        private Foo Create(int a, int b)
        {
            return new Foo(a, b);
        }
    }
}";
            testCode = testCode.AssertReplace("new Foo(a, b)", call);
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ConstructorCallWithNamedArgumentsOnSameRow()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }

        private Foo Create(int a, int b, int c, int d)
        {
            return new Foo(a: a, b: b, c: c, d: d);
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ConstructorCallWithArgumentsOnSameRow()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }

        private Foo Create(int a, int b, int c, int d)
        {
            return new Foo(a, b, c, d);
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ConstructorCallWithNamedArgumentsOnSeparateRows()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }

        private Foo Create(int a, int b, int c, int d)
        {
            return new Foo(
                a: a, 
                b: b, 
                c: c, 
                d: d);
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoresStringFormat()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Globalization;

    public static class Foo
    {
        private static string Bar(int a, int b, int c, int d)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                ""{0}{1}{2}{3}"",
                a,
                b,
                c,
                d);
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoresWhendifferentTypes()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a, double b, string c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public double B { get; }

        public string C { get; }

        public int D { get; }

        private Foo Create(int a, double b, string c, int d)
        {
            return new Foo(
                a, 
                b, 
                c, 
                d);
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoresWhenInExpressionTree()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Linq.Expressions;

    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }

        private Expression<Func<Foo>> Create(int a, int b, int c, int d)
        {
            return () => new Foo(
                a,
                b,
                c,
                d);
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
