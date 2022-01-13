namespace Gu.Analyzers.Test.GU0008AvoidRelayPropertiesTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class Diagnostics
{
    private static readonly PropertyDeclarationAnalyzer Analyzer = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0008AvoidRelayProperties);

    [TestCase("this.bar.Value;")]
    [TestCase("bar.Value;")]
    public static void WhenReturningPropertyOfInjectedField(string getter)
    {
        var code = @"
namespace N
{
    public class C1
    {
        private readonly C2 bar;

        public C1(C2 bar)
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
        var c2 = @"
namespace N
{
    public class C2
    {
        public int Value { get; }
    }
}";
        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code, c2);
    }

    [TestCase("this.bar.Value;")]
    [TestCase("bar.Value;")]
    public static void WhenReturningPropertyOfFieldExpressionBody(string body)
    {
        var code = @"
namespace N
{
    public class C1
    {
        private readonly C2 bar;

        public C1(C2 bar)
        {
            this.bar = bar;
        }

        public int Value => ↓this.bar.Value;
    }
}".AssertReplace("this.bar.Value;", body);
        var c2 = @"
namespace N
{
    public class C2
    {
        public int Value { get; }
    }
}";
        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code, c2);
    }
}
