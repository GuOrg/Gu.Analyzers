namespace Gu.Analyzers.Test.GU0013CheckNameInThrowTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class CodeFix
    {
        private static readonly ObjectCreationAnalyzer Analyzer = new ObjectCreationAnalyzer();
        private static readonly ThrowForCorrectParameterFix Fix = new ThrowForCorrectParameterFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0013TrowForCorrectParameter.Descriptor);

        [Test]
        public static void ThrowExpressionNameofWrong()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly string bar;

        public Foo(string bar)
        {
            this.bar = bar ?? throw new ArgumentNullException(nameof(↓Foo));
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly string bar;

        public Foo(string bar)
        {
            this.bar = bar ?? throw new ArgumentNullException(nameof(bar));
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode, fixTitle: "Use parameter bar.");
        }

        [Test]
        public static void ThrowExpressionNameofOther()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly string sq;

        public Foo(string s1, string s2)
        {
            this.sq = s1 ?? throw new ArgumentNullException(nameof(↓s2));
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly string sq;

        public Foo(string s1, string s2)
        {
            this.sq = s1 ?? throw new ArgumentNullException(nameof(s1));
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode, fixTitle: "Use parameter s1.");
        }

        [Test]
        public static void ThrowExpressionStringLiteral()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly string bar;

        public Foo(string bar)
        {
            this.bar = bar ?? throw new ArgumentNullException(↓""Foo"");
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly string bar;

        public Foo(string bar)
        {
            this.bar = bar ?? throw new ArgumentNullException(nameof(bar));
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode, fixTitle: "Use parameter bar.");
        }
    }
}
