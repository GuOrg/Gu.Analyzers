namespace Gu.Analyzers.Test.GU0008AvoidRelayPropertiesTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class ValidCode
    {
        private static readonly PropertyDeclarationAnalyzer Analyzer = new PropertyDeclarationAnalyzer();

        [TestCase("public int Value { get; set; }")]
        [TestCase("public int Value { get; } = 1;")]
        public static void AutoProp(string property)
        {
            var fooCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public int Value { get; set; }
    }
}".AssertReplace("public int Value { get; set; }", property);
            RoslynAssert.Valid(Analyzer, fooCode);
        }

        [TestCase("this.value;")]
        [TestCase("value;")]
        public static void ExpressionBodyReturningField(string getter)
        {
            var fooCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int value = 2;

        public int Value => this.value;
    }
}".AssertReplace("this.value;", getter);
            RoslynAssert.Valid(Analyzer, fooCode);
        }

        [TestCase("get { return this.value; }")]
        [TestCase("get { return value; }")]
        public static void WithBackingField(string getter)
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
}".AssertReplace("get { return this.value; }", getter);
            RoslynAssert.Valid(Analyzer, fooCode);
        }

        [TestCase("return this.bar.Value;")]
        [TestCase("return bar.Value;")]
        public static void WhenReturningPropertyOfCreatedField(string getter)
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
}".AssertReplace("return this.bar.Value;", getter);
            var barCode = @"
namespace RoslynSandbox
{
    public class Bar
    {
        public int Value { get; }
    }
}";
            RoslynAssert.Valid(Analyzer, fooCode, barCode);
        }

        [TestCase("this.bar.Value;")]
        [TestCase("bar.Value;")]
        public static void WhenReturningPropertyOfCreatedFieldExpressionBody(string getter)
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
}".AssertReplace("this.bar.Value;", getter);
            var barCode = @"
namespace RoslynSandbox
{
    public class Bar
    {
        public int Value { get; }
    }
}";
            RoslynAssert.Valid(Analyzer, fooCode, barCode);
        }
    }
}
