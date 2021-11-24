namespace Gu.Analyzers.Test.GU0002NamedArgumentPositionMatchesTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Valid
    {
        private static readonly ArgumentListAnalyzer Analyzer = new();

        [TestCase("new C(a, b)")]
        [TestCase("new C(a: a, b: b)")]
        [TestCase("new C(a, b: b)")]
        public static void ConstructorCallWithTwoArguments(string call)
        {
            var code = @"
namespace N
{
    public class C
    {
        public C(int a, int b)
        {
            this.A = a;
            this.B = b;
        }

        public int A { get; }

        public int B { get; }

        private C Create(int a, int b)
        {
            return new C(a, b);
        }
    }
}".AssertReplace("new C(a, b)", call);

            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("new Foo(a, b)")]
        [TestCase("new Foo(a: a, b: b)")]
        [TestCase("new Foo(a, b: b)")]
        public static void ConstructorCallWithTwoArgumentsStruct(string call)
        {
            var code = @"
namespace N
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
}".AssertReplace("new Foo(a, b)", call);

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ConstructorCallWithNamedArgumentsOnSameRow()
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ConstructorCallWithArgumentsOnSameRow()
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ConstructorCallWithNamedArgumentsOnSeparateRows()
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoresStringFormat()
        {
            var code = @"
namespace N
{
    using System.Globalization;

    public static class C
    {
        private static string M(int a, int b, int c, int d)
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoresWhendifferentTypes()
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoresWhenInExpressionTree()
        {
            var code = @"
namespace N
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

            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
