namespace Gu.Analyzers.Test.GU0008AvoidRelayPropertiesTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class CodeFix
    {
        [TestCase("return this.bar.Value;")]
        [TestCase("return bar.Value;")]
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

        ↓public int Value
        { 
            get
            {
                return this.bar.Value;
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
            fooCode = fooCode.AssertReplace("return this.bar.Value;", getter);
            AnalyzerAssert.Diagnostics<GU0008AvoidRelayProperties>(fooCode, barCode);
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

        ↓public int Value => this.bar.Value;
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
            AnalyzerAssert.Diagnostics<GU0008AvoidRelayProperties>(fooCode, barCode);
        }
    }
}
