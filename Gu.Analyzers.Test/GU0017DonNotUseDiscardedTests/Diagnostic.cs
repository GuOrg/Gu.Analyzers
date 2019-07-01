namespace Gu.Analyzers.Test.GU0017DonNotUseDiscardedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class Diagnostic
    {
        private static readonly DiagnosticAnalyzer Analyzer = new IdentifierNameAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0017DonNotUseDiscarded.Descriptor);

        [TestCase("↓_ + 3")]
        [TestCase("↓_++")]
        [TestCase("↓_.ToString()")]
        [TestCase("Console.WriteLine(↓_)")]
        public static void Local(string expression)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        public C()
        {
            var _ = 1;
            _ = ↓_ + 3;
        }
    }
}".AssertReplace("↓_ + 3", expression);
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void OneParameterLambda()
        {
            var testCode = @"
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
                      .Subscribe(_ => _.ToString());
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void TwoParameterLambda()
        {
            var testCode = @"
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
