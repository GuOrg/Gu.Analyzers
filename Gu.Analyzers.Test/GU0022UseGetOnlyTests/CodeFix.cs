namespace Gu.Analyzers.Test.GU0022UseGetOnlyTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Codefix
    {
        private static readonly GU0022UseGetOnly Analyzer = new GU0022UseGetOnly();
        private static readonly UseGetOnlyCodeFixProvider CodeFix = new UseGetOnlyCodeFixProvider();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0022");

        [Test]
        public void InitializedInCtor()
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

        public int A { get; ↓private set; }

        public int B { get; }

        public int C { get; }

        public int D { get; }
    }
}";

            var fixedCode = @"
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
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void InitializedInCtorAndPropertyInitializer()
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

        public int A { get; ↓private set; } = 2;

        public int B { get; }

        public int C { get; }

        public int D { get; }
    }
}";

            var fixedCode = @"
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

        public int A { get; } = 2;

        public int B { get; }

        public int C { get; }

        public int D { get; }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, CodeFix, ExpectedDiagnostic, testCode, fixedCode);
        }
    }
}
