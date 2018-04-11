namespace Gu.Analyzers.Test.GU0008AvoidRelayPropertiesTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        private static readonly PropertyDeclarationAnalyzer Analyzer = new PropertyDeclarationAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0008");

        [TestCase("this.bar.Value;")]
        [TestCase("bar.Value;")]
        public void WhenReturningPropertyOfInjectedField(string getter)
        {
            var fooCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly Bar bar;

        public Foo(Bar bar)
        {
            this.bar = bar;
        }

        public int Value
        { 
            get
            {
                return ↓this.bar.Value;
            }
        }
    }
}";
            var barCode = @"
namespace RoslynSandbox
{
    public class Bar
    {
        public int Value { get; }
    }
}";
            fooCode = fooCode.AssertReplace("this.bar.Value;", getter);
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, fooCode, barCode);
        }

        [TestCase("this.bar.Value;")]
        [TestCase("bar.Value;")]
        public void WhenReturningPropertyOfFieldExpressionBody(string body)
        {
            var fooCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly Bar bar;

        public Foo(Bar bar)
        {
            this.bar = bar;
        }

        public int Value => ↓this.bar.Value;
    }
}";
            var barCode = @"
namespace RoslynSandbox
{
    public class Bar
    {
        public int Value { get; }
    }
}";
            fooCode = fooCode.AssertReplace("this.bar.Value;", body);
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, fooCode, barCode);
        }
    }
}
