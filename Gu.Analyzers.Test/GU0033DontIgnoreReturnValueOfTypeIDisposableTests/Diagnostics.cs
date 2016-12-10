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
            var disposableCode = @"
using System;
class Disposable : IDisposable
{
    public void Dispose()
	{
	}
}";

            var testCode = @"
public sealed class Foo
{
    public void Meh()
    {
        ↓new Disposable();
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore returnvalue of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(new[] { disposableCode, testCode }, expected, CancellationToken.None).ConfigureAwait(false);
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

        [Test]
        public async Task IgnoringNewDisposabledPassedIntoCtor()
        {
            var disposableCode = @"
using System;
class Disposable : IDisposable
{
    public void Dispose()
	{
	}
}";
            var barCode = @"
using System;

public class Bar
{
    private readonly IDisposable disposable;

    public Bar(IDisposable disposable)
    {
       this.disposable = disposable;
    }
}";

            var testCode = @"
public sealed class Foo
{
    public Bar Meh()
    {
        return new Bar(↓new Disposable());
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore returnvalue of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(new[] { disposableCode, barCode, testCode }, expected, CancellationToken.None).ConfigureAwait(false);
        }
    }
}