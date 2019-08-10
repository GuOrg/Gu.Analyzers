namespace Gu.Analyzers.Test.GU0002NamedArgumentPositionMatchesTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class NoFix
    {
        private static readonly ArgumentListAnalyzer Analyzer = new ArgumentListAnalyzer();
        private static readonly MoveArgumentFix Fix = new MoveArgumentFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0002NamedArgumentPositionMatches);

        [Test]
        public static void ConstructorIgnoredIfNonWhitespaceTrivia()
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
                b: b, // some comment
                a: a,
                c: c,
                d: d);
        }
    }
}";
            RoslynAssert.NoFix(Analyzer, Fix, ExpectedDiagnostic, testCode);
        }
    }
}
