namespace Gu.Analyzers.Test.GU0008AvoidRelayPropertiesTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class Valid
{
    private static readonly PropertyDeclarationAnalyzer Analyzer = new();

    [TestCase("public int Value { get; set; }")]
    [TestCase("public int Value { get; } = 1;")]
    public static void AutoProp(string property)
    {
        var code = @"
namespace N
{
    public class C
    {
        public int Value { get; set; }
    }
}".AssertReplace("public int Value { get; set; }", property);
        RoslynAssert.Valid(Analyzer, code);
    }

    [TestCase("this.value;")]
    [TestCase("value;")]
    public static void ExpressionBodyReturningField(string getter)
    {
        var code = @"
namespace N
{
    public class C
    {
        private readonly int value = 2;

        public int Value => this.value;
    }
}".AssertReplace("this.value;", getter);
        RoslynAssert.Valid(Analyzer, code);
    }

    [TestCase("get { return this.value; }")]
    [TestCase("get { return value; }")]
    public static void WithBackingField(string getter)
    {
        var code = @"
namespace N
{
    public class C
    {
        private int value;

        public int Value
        {
            get { return this.value; }
            set { this.value = value; }
        }
    }
}".AssertReplace("get { return this.value; }", getter);
        RoslynAssert.Valid(Analyzer, code);
    }

    [TestCase("return this.bar.Value;")]
    [TestCase("return bar.Value;")]
    public static void WhenReturningPropertyOfCreatedField(string getter)
    {
        var c1 = @"
namespace N
{
    public class C1
    {
        private readonly Bar bar;

        public C1(Bar bar)
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
        var bar = @"
namespace N
{
    public class Bar
    {
        public int Value { get; }
    }
}";
        RoslynAssert.Valid(Analyzer, c1, bar);
    }

    [TestCase("this.c2.P;")]
    [TestCase("c2.P;")]
    public static void WhenReturningPropertyOfCreatedFieldExpressionBody(string getter)
    {
        var fooCode = @"
namespace N
{
    public class C1
    {
        private readonly C2 c2;

        public C1()
        {
            this.c2 = new C2();
        }

        public int P => this.c2.P;
    }
}".AssertReplace("this.c2.P;", getter);
        var c2 = @"
namespace N
{
    public class C2
    {
        public int P { get; }
    }
}";
        RoslynAssert.Valid(Analyzer, fooCode, c2);
    }
}
