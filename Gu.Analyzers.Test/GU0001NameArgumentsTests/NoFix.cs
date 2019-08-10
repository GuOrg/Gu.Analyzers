﻿namespace Gu.Analyzers.Test.GU0001NameArgumentsTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class NoFix
    {
        private static readonly ArgumentListAnalyzer Analyzer = new ArgumentListAnalyzer();
        private static readonly NameArgumentsFix Fix = new NameArgumentsFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0001NameArguments);

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
