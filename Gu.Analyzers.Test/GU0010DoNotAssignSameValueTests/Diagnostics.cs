namespace Gu.Analyzers.Test.GU0010DoNotAssignSameValueTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Diagnostics
    {
        private static readonly SimpleAssignmentAnalyzer Analyzer = new SimpleAssignmentAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0010DoNotAssignSameValue.Descriptor);

        [TestCase("this.A = this.A;", "this.A = this.A;")]
        [TestCase("this.A = this.A;", "this.A = A;")]
        [TestCase("this.A = this.A;", "A = A;")]
        [TestCase("this.A = this.A;", "A = this.A;")]
        public static void SetPropertyToSelf(string before, string after)
        {
            var code = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public int A { get; private set; }

        private void Bar()
        {
            ↓this.A = this.A;
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic.WithMessage("Assigning made to same, did you mean to assign something else?"), code);
        }

        [Test]
        public static void SetPropertyToSelfWithThis()
        {
            var code = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public int A { get; private set; }

        private void Bar()
        {
            ↓this.A = this.A;
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }
    }
}
