namespace Gu.Analyzers.Test.GU0013CheckNameInThrowTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class CodeFix
    {
        private static readonly ObjectCreationAnalyzer Analyzer = new ObjectCreationAnalyzer();
        private static readonly ThrowForCorrectParameterCodeFixProvider Fix = new ThrowForCorrectParameterCodeFixProvider();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0013");

        [Test]
        public void ThrowExpression()
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
            this.bar = bar ?? throw new ArgumentNullException(↓nameof(Foo));
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
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }
    }
}