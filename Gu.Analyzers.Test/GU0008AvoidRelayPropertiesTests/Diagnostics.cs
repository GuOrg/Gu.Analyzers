namespace Gu.Analyzers.Test.GU0008AvoidRelayPropertiesTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Diagnostics
    {
        private static readonly PropertyDeclarationAnalyzer Analyzer = new PropertyDeclarationAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0008AvoidRelayProperties.Descriptor);

        [TestCase("this.bar.Value;")]
        [TestCase("bar.Value;")]
        public static void WhenReturningPropertyOfInjectedField(string getter)
        {
            var fooCode = @"
namespace N
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
}".AssertReplace("this.bar.Value;", getter);
            var code = @"
namespace N
{
    public class Bar
    {
        public int Value { get; }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, fooCode, code);
        }

        [TestCase("this.bar.Value;")]
        [TestCase("bar.Value;")]
        public static void WhenReturningPropertyOfFieldExpressionBody(string body)
        {
            var fooCode = @"
namespace N
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
}".AssertReplace("this.bar.Value;", body);
            var code = @"
namespace N
{
    public class Bar
    {
        public int Value { get; }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, fooCode, code);
        }
    }
}
