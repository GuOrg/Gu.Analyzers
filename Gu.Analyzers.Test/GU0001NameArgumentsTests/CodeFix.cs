namespace Gu.Analyzers.Test.GU0001NameArgumentsTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class CodeFix
    {
        private static readonly ArgumentListAnalyzer Analyzer = new ArgumentListAnalyzer();
        private static readonly NameArgumentsFix Fix = new NameArgumentsFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0001NameArguments.Descriptor);

        [Test]
        public static void Message()
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

        private Foo[] Create(int a, int b, int c, int d)
        {
            return new[]
                       {
                           new Foo↓(
                               a,
                               b,
                               c,
                               d)
                       };
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic.WithMessage("Name the arguments."), code);
        }

        [Test]
        public static void Constructor()
        {
            var before = @"
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
            return new Foo↓(
                a, 
                b, 
                c, 
                d);
        }
    }
}";

            var after = @"
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void ConstructorInArrayInitializer()
        {
            var before = @"
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

        private Foo[] Create(int a, int b, int c, int d)
        {
            return new[]
                       {
                           new Foo↓(
                               a,
                               b,
                               c,
                               d)
                       };
        }
    }
}";

            var after = @"
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

        private Foo[] Create(int a, int b, int c, int d)
        {
            return new[]
                       {
                           new Foo(
                               a: a,
                               b: b,
                               c: c,
                               d: d)
                       };
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void ConstructorInFunc()
        {
            var before = @"
namespace N
{
    using System;

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

        private Func<Foo> Create(int a, int b, int c, int d)
        {
            return () => new Foo↓(
                a,
                b,
                c,
                d);
        }
    }
}";

            var after = @"
namespace N
{
    using System;

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

        private Func<Foo> Create(int a, int b, int c, int d)
        {
            return () => new Foo(
                a: a,
                b: b,
                c: c,
                d: d);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void ConstructorIgnoredIfAnyNamed()
        {
            var testCode = @"
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
            return new Foo↓(
               a,
               b,
               c,
               d: d);
        }
    }
}";
            RoslynAssert.NoFix(Analyzer, Fix, ExpectedDiagnostic, testCode);
        }
    }
}
