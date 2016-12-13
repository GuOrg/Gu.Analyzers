namespace Gu.Analyzers.Test.GU0032DisposeBeforeReassigningTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class Diagnostics : DiagnosticVerifier<GU0032DisposeBeforeReassigning>
    {
        [Test]
        public async Task NotDisposingVariable()
        {
            var testCode = @"
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            var stream = File.OpenRead(string.Empty);
            ↓stream = File.OpenRead(string.Empty);
        }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose before re-assigning.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        [Explicit("meh")]
        public async Task NotDisposingFieldInCtor()
        {
            var testCode = @"
    using System;
    using System.IO;

    public class Foo
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        public Foo()
        {
            ↓stream = File.OpenRead(string.Empty);
        }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose before re-assigning.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingFieldInMethod()
        {
            var testCode = @"
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public void Meh()
        {
            ↓stream = File.OpenRead(string.Empty);
        }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose before re-assigning.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }
    }
}