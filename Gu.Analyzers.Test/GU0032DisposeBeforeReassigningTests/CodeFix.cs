namespace Gu.Analyzers.Test.GU0032DisposeBeforeReassigningTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class CodeFix : CodeFixVerifier<GU0032DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>
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

            // keeping it safe and doing ?.Dispose()
            // will require some work to figure out if it can be null
            var fixedCode = @"
using System.IO;

public class Foo
{
    public void Meh()
    {
        var stream = File.OpenRead(string.Empty);
        stream?.Dispose();
        stream = File.OpenRead(string.Empty);
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AssigningViaOutParameter()
        {
            var testCode = @"
    using System;
    using System.IO;

    public class Foo
    {
        public void Update()
        {
            var stream = File.OpenRead(string.Empty);
            ↓TryGetStream(out stream);
        }

        public bool TryGetStream(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose before re-assigning.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
    using System;
    using System.IO;

    public class Foo
    {
        public void Update()
        {
            var stream = File.OpenRead(string.Empty);
            stram?.Dispose();
            TryGetStream(out stream);
        }

        public bool TryGetStream(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AssigningOutParameterTwice()
        {
            var testCode = @"
    using System;
    using System.IO;

    public class Foo
    {
        public bool TryGetStream(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            ↓stream = File.OpenRead(string.Empty);
            return true;
        }
    }";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose before re-assigning.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
    using System;
    using System.IO;

    public class Foo
    {
        public bool TryGetStream(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            stream?.Dispose();
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task RefParameter()
        {
            var testCode = @"
using System;
using System.IO;

public class Foo
{
    public bool TryGetStream(ref Stream stream)
    {
        ↓stream = File.OpenRead(string.Empty);
        return true;
    }
}";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose before re-assigning.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public class Foo
{
    public bool TryGetStream(ref Stream stream)
    {
        stream?.Dispose();
        stream = File.OpenRead(string.Empty);
        return true;
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true).ConfigureAwait(false);
        }

        [Test]
        [Explicit("Fix?")]
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

            var fixedCode = @"
using System;
using System.IO;

public class Foo
{
    private Stream stream;

    public void Meh()
    {
        stream?.Dispose();
        stream = File.OpenRead(string.Empty);
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}