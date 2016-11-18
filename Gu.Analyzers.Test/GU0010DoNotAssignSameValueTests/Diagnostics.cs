namespace Gu.Analyzers.Test.GU0010DoNotAssignSameValueTests
{
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class Diagnostics : DiagnosticVerifier<GU0010DoNotAssignSameValue>
    {
        [Test]
        public async Task SetPropertyToSelf()
        {
            var testCode = @"
    public class Foo
    {
        public int A { get; private set; }

        private void Bar()
        {
            ↓A = A;
        }
    }";

            var expected = this.CSharpDiagnostic().WithLocationIndicated(ref testCode).WithMessage("Assigning same value.");
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

            var expected = this.CSharpDiagnostic().WithLocationIndicated(ref testCode).WithMessage("Assigning same value.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
        }
    }
}