namespace Gu.Analyzers.Test.GU0015DoNotAssignMoreThanOnceTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class Diagnostics
{
    private static readonly SimpleAssignmentAnalyzer Analyzer = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0015DoNotAssignMoreThanOnce);

    [Test]
    public static void FieldInConstructor()
    {
        var code = @"
namespace N
{
    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = text;
            ↓this.text = text;
            var length = this.text.ToString();
        }
    }
}";
        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
    }

    [Test]
    public static void PropertyInConstructor()
    {
        var code = @"
namespace N
{
    public class C
    {
        public C(string text)
        {
            this.Text = text;
            ↓this.Text = text;
            var length = this.Text.Length;
        }

        public string Text { get; }
    }
}";

        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
    }

    [Test]
    public static void FieldInMethod()
    {
        var code = @"
namespace N
{
    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = text;
            ↓this.text = text;
            var length = this.text.ToString();
        }
    }
}";
        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
    }
}
