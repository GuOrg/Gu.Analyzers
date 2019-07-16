namespace Gu.Analyzers.Test.GU0015DoNotAssignMoreThanOnceTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class Diagnostic
    {
        private static readonly DiagnosticAnalyzer Analyzer = new SimpleAssignmentAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0015DoNotAssignMoreThanOnce.Descriptor);

        [Test]
        public static void FieldInConstructor()
        {
            var code = @"
namespace N
{
    public class Foo
    {
        private readonly string text;

        public Foo(string text)
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
    public class Foo
    {
        public Foo(string text)
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
    public class Foo
    {
        private readonly string text;

        public Foo(string text)
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
}
