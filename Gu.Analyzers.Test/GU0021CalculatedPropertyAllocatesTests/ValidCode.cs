namespace Gu.Analyzers.Test.GU0021CalculatedPropertyAllocatesTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class ValidCode
    {
        private static readonly PropertyDeclarationAnalyzer Analyzer = new PropertyDeclarationAnalyzer();

        [Test]
        public static void ArrowAdd()
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

        public int Sum => this.A + this.B + this.C + this.D;
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ArrowStruct()
        {
            var code = @"
namespace N
{
    public struct Foo
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

        public Foo Sum => new Foo(this.A, this.B, this.C, this.D);
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ExpressionBodyMethodIsNoError()
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

        public Foo Bar() => new Foo(this.A, this.B, this.C, this.D);
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
