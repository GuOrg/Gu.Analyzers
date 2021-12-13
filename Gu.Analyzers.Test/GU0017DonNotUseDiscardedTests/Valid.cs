namespace Gu.Analyzers.Test.GU0017DonNotUseDiscardedTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class Valid
    {
        private static readonly DiagnosticAnalyzer Analyzer = new IdentifierNameAnalyzer();

        [Test]
        public static void DiscardSymbol()
        {
            var code = @"
namespace N
{
    public class C
    {
        public C()
        {
            _ = 1;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void Local()
        {
            var code = @"
namespace N
{
    public class C
    {
        public C()
        {
#pragma warning disable CS0219
            var _ = 1;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("out _")]
        [TestCase("out var _")]
        public static void Out(string arg)
        {
            var code = @"
namespace N
{
    public class C
    {
        public C(string text)
        {
            int.TryParse(text, out _);
        }
    }
}".AssertReplace("out _", arg);
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void OneParameterLambda()
        {
            var code = @"
namespace N
{
    using System;
    using System.Reactive;
    using System.Reactive.Linq;

    public class C
    {
        public C()
        {
            Observable.Never<Unit>()
                      .Subscribe(_ => { });
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void TwoParameterLambda()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public C()
        {
            Console.CancelKeyPress += (_, __) => { };
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
