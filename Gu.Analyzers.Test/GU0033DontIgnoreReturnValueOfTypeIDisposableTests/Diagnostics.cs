namespace Gu.Analyzers.Test.GU0033DontIgnoreReturnValueOfTypeIDisposableTests
{
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class Diagnostics : DiagnosticVerifier<GU0033DontIgnoreReturnValueOfTypeIDisposable>
    {
        [Test]
        public async Task IgnoringFileOpenRead()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo
{
    public void Meh()
    {
        ↓File.OpenRead("""");
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore returnvalue of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoringNewDisposable()
        {
            var testCode = @"
using System;
using System.IO;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}

public sealed class Foo
{
    public void Meh()
    {
        ↓File.OpenRead("""");
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore returnvalue of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoringFileOpenReadPassedIntoCtor()
        {
            var testCode = @"
using System;
using System.IO;

public class Bar
{
    private readonly Stream stream;

    public Bar(Stream stream)
    {
       this.stream = stream;
    }
}

public sealed class Foo
{
    public Bar Meh()
    {
        return new Bar(↓File.OpenRead(""""));
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore returnvalue of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
        }
    }
}