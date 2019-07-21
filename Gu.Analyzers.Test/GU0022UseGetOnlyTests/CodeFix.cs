namespace Gu.Analyzers.Test.GU0022UseGetOnlyTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class Codefix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new GU0022UseGetOnly();
        private static readonly CodeFixProvider Fix = new UseGetOnlyFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0022UseGetOnly.Descriptor);

        [Test]
        public static void InitializedInCtor()
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

        public int A { get; ↓private set; }

        public int B { get; }

        public int C { get; }

        public int D { get; }
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
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void InitializedInCtorAndPropertyInitializer()
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

        public int A { get; ↓private set; } = 2;

        public int B { get; }

        public int C { get; }

        public int D { get; }
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

        public int A { get; } = 2;

        public int B { get; }

        public int C { get; }

        public int D { get; }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }
    }
}
