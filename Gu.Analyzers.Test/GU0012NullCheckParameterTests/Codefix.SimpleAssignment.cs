namespace Gu.Analyzers.Test.GU0012NullCheckParameterTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        internal class SimpleAssignment
        {
            private static readonly SimpleAssignmentAnalyzer Analyzer = new SimpleAssignmentAnalyzer();
            private static readonly NullCheckParameterCodeFixProvider Fix = new NullCheckParameterCodeFixProvider();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0012");

            [TestCase("public")]
            [TestCase("internal")]
            [TestCase("protected")]
            public void ConstructorFullyQualified(string access)
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly string text;

        public Foo(string text)
        {
            this.text = ↓text;
        }
    }
}";
                testCode = testCode.AssertReplace("public Foo", $"{access} Foo");
                var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly string text;

        public Foo(string text)
        {
            this.text = text ?? throw new System.ArgumentNullException(nameof(text));
        }
    }
}";
                fixedCode = fixedCode.AssertReplace("public Foo", $"{access} Foo");
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void PublicCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly string text;

        public Foo(string text)
        {
            this.text = ↓text;
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly string text;

        public Foo(string text)
        {
            this.text = text ?? throw new ArgumentNullException(nameof(text));
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }
        }
    }
}