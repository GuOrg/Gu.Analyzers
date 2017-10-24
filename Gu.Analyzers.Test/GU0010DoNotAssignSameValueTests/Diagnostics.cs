namespace Gu.Analyzers.Test.GU0010DoNotAssignSameValueTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics : DiagnosticVerifier<GU0010DoNotAssignSameValue>
    {
        [TestCase("this.A = this.A;", "this.A = this.A;")]
        [TestCase("this.A = this.A;", "this.A = A;")]
        [TestCase("this.A = this.A;", "A = A;")]
        [TestCase("this.A = this.A;", "A = this.A;")]
        public void SetPropertyToSelf(string before, string after)
        {
            var testCode = @"
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

            var expectedDiagnostic = ExpectedDiagnostic.CreateFromCodeWithErrorsIndicated(
                diagnosticId: "GU0010",
                message: "Assigning made to same, did you mean to assign something else?",
                code: testCode,
                cleanedSources: out testCode);
            AnalyzerAssert.Diagnostics<GU0010DoNotAssignSameValue>(expectedDiagnostic, testCode);
        }

        [Test]
        public void SetPropertyToSelfWithThis()
        {
            var testCode = @"
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

            AnalyzerAssert.Diagnostics<GU0010DoNotAssignSameValue>(testCode);
        }
    }
}