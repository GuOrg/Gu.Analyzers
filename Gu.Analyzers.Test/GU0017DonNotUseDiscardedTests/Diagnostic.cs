namespace Gu.Analyzers.Test.GU0017DonNotUseDiscardedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class Diagnostic
    {
        private static readonly DiagnosticAnalyzer Analyzer = new IdentifierNameAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0017DonNotUseDiscarded.Descriptor);

        [TestCase("var o = ↓_ + 3;")]
        [TestCase("var o = ↓_++;")]
        [TestCase("var o = ↓_.ToString();")]
        [TestCase("Console.WriteLine(↓_);")]
        public static void Local(string statement)
        {
            var _ = 1;
            var code = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        public C()
        {
            var _ = 1;
            var o = ↓_ + 3;
        }
    }
}".AssertReplace("var o = ↓_ + 3;", statement);
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void OneParameterLambda()
        {
            var code = @"
namespace RoslynSandbox
{
    using System;
    using System.Reactive;
    using System.Reactive.Linq;

    public class C
    {
        public C()
        {
            Observable.Never<Unit>()
                      .Subscribe(_ => ↓_.ToString());
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void TwoParameterLambda()
        {
            var code = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        public C()
        {
            Console.CancelKeyPress += (_, __) => Console.WriteLine(↓_);
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }
    }
}
