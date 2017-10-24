namespace Gu.Analyzers.Test.GU0008AvoidRelayPropertiesTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly GU0008AvoidRelayProperties Analyzer = new GU0008AvoidRelayProperties();

        [TestCase("public int Value { get; set; }")]
        [TestCase("public int Value { get; } = 1;")]
        public void AutoProp(string property)
        {
            var fooCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public int Value { get; set; }
    }
}";

            fooCode = fooCode.AssertReplace("public int Value { get; set; }", property);
            AnalyzerAssert.Valid(Analyzer, fooCode);
        }

        [TestCase("this.value;")]
        [TestCase("value;")]
        public void ExpressionBodyReturningField(string getter)
        {
            var fooCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int value = 2;

        public int Value => this.value;
    }
}";
            fooCode = fooCode.AssertReplace("this.value;", getter);
            AnalyzerAssert.Valid(Analyzer, fooCode);
        }

        [TestCase("get { return this.value; }")]
        [TestCase("get { return value; }")]
        public void WithBackingField(string getter)
        {
            var fooCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private int value;

        public int Value
        {
            get { return this.value; }
            set { this.value = value; }
        }
    }
}";
            fooCode = fooCode.AssertReplace("get { return this.value; }", getter);
            AnalyzerAssert.Valid(Analyzer, fooCode);
        }

        [TestCase("return this.bar.Value;")]
        [TestCase("return bar.Value;")]
        public void WhenReturningPropertyOfCreatedField(string getter)
        {
            var fooCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly Bar bar;

        public Foo(Bar bar)
        {
            this.bar = new Bar();
        }

        public int Value
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
            AnalyzerAssert.Valid(Analyzer, fooCode, barCode);
        }

        [TestCase("this.bar.Value;")]
        [TestCase("bar.Value;")]
        public void WhenReturningPropertyOfCreatedFieldExpressionBody(string getter)
        {
            var fooCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly Bar bar;

        public Foo(Bar bar)
        {
            this.bar = new Bar();
        }

        public int Value => this.bar.Value;
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
            AnalyzerAssert.Valid(Analyzer, fooCode, barCode);
        }
    }
}