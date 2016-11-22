namespace Gu.Analyzers.Test.GU0010DoNotAssignSameValueTests
{
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class Diagnostics : DiagnosticVerifier<GU0010DoNotAssignSameValue>
    {
        [TestCase("this.A = this.A;", "this.A = this.A;")]
        [TestCase("this.A = this.A;", "this.A = A;")]
        [TestCase("this.A = this.A;", "A = A;")]
        [TestCase("this.A = this.A;", "A = this.A;")]
        public async Task SetPropertyToSelf(string before, string after)
        {
            var testCode = @"
    public class Foo
    {
        public int A { get; private set; }

        private void Bar()
        {
            ↓this.A = this.A;
        }
    }";
            testCode = testCode.AssertReplace(before, after);
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Assigning made to same, did you mean to assign something else?");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
        }

        [Test]
        public async Task SetPropertyToSelfWithThis()
        {
            var testCode = @"
    public class Foo
    {
        public int A { get; private set; }

        private void Bar()
        {
            ↓this.A = this.A;
        }
    }";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Assigning made to same, did you mean to assign something else?");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
        }
    }
}