namespace Gu.Analyzers.Test.GU0038DontReturnInjectedAndCreatedTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class Diagnostics : DiagnosticVerifier<GU0038DontReturnInjectedAndCreated>
    {
        [Test]
        public async Task If()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        ↓public Stream Get(string text)
        {
            if (text == null)
            {
               return this.stream;
            }

            return File.OpenRead(text);
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't return injected or cached and created disposables.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task NullCoalesce()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        ↓public Stream Get(string text)
        {
            return this.stream ?? File.OpenRead(text);
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't return injected or cached and created disposables.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task Ternary()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        ↓public Stream Get(string text)
        {
            return text == null
                       ? this.stream
                       : File.OpenRead(text);
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't return injected or cached and created disposables.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }
    }
}