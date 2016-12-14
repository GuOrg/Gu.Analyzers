namespace Gu.Analyzers.Test.GU0034ReturntypeShouldIndicateIDisposableTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class Diagnostics : DiagnosticVerifier<GU0034ReturntypeShouldIndicateIDisposable>
    {
        [Test]
        public async Task ReturnFileOpenReadAsObject()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo
{
    public object Meh()
    {
        return ↓File.OpenRead(string.Empty);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Returntype should indicate that the value should be disposed.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task ReturnFileOpenReadAsObjectExpressionBody()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo
{
    public object Meh() => ↓File.OpenRead(string.Empty);
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Returntype should indicate that the value should be disposed.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task PropertyReturnFileOpenReadAsObjectExpressionBody()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo
{
    public object Meh => ↓File.OpenRead(string.Empty);
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Returntype should indicate that the value should be disposed.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }
    }
}