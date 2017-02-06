namespace Gu.Analyzers.Test.GU0032DisposeBeforeReassigningTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class CodeFix : CodeFixVerifier<GU0032DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>
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
        public async Task AssigningParameterTwice()
        {
            var testCode = @"
using System;
using System.IO;

public class Foo
{
    public void Bar(Stream stream)
    {
        stream = File.OpenRead(string.Empty);
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
    public void Bar(Stream stream)
    {
        stream = File.OpenRead(string.Empty);
        stream?.Dispose();
        stream = File.OpenRead(string.Empty);
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AssigningInIfElse()
        {
            var testCode = @"
using System.IO;

public class Foo
{
    public void Meh()
    {
        Stream stream = File.OpenRead(string.Empty);
        if (true)
        {
            stream.Dispose();
            stream = File.OpenRead(string.Empty);
        }
        else
        {
            ↓stream = File.OpenRead(string.Empty);
        }
    }
}";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose before re-assigning.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
using System.IO;

public class Foo
{
    public void Meh()
    {
        Stream stream = File.OpenRead(string.Empty);
        if (true)
        {
            stream.Dispose();
            stream = File.OpenRead(string.Empty);
        }
        else
        {
            stream?.Dispose();
            stream = File.OpenRead(string.Empty);
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingInitializedFieldInCtor()
        {
            var testCode = @"
using System;
using System.IO;

public class Foo
{
    private readonly Stream stream = File.OpenRead(string.Empty);

    public Foo()
    {
        ↓this.stream = File.OpenRead(string.Empty);
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
    private readonly Stream stream = File.OpenRead(string.Empty);

    public Foo()
    {
        this.stream?.Dispose();
        this.stream = File.OpenRead(string.Empty);
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingInitializedPropertyInCtor()
        {
            var testCode = @"
using System;
using System.IO;

public class Foo
{
    public Foo()
    {
        ↓this.Stream = File.OpenRead(string.Empty);
    }

    public Stream Stream { get; } = File.OpenRead(string.Empty);
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
    public Foo()
    {
        this.Stream?.Dispose();
        this.Stream = File.OpenRead(string.Empty);
    }

    public Stream Stream { get; } = File.OpenRead(string.Empty);
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingInitializedBackingFieldInCtor()
        {
            var testCode = @"
using System;
using System.IO;

public class Foo
{
    private Stream stream = File.OpenRead(string.Empty);

    public Foo()
    {
        ↓this.Stream = File.OpenRead(string.Empty);
    }

    public Stream Stream
    {
        get { return this.stream; }
        set { this.stream = value; }
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
    private Stream stream = File.OpenRead(string.Empty);

    public Foo()
    {
        this.Stream?.Dispose();
        this.Stream = File.OpenRead(string.Empty);
    }

    public Stream Stream
    {
        get { return this.stream; }
        set { this.stream = value; }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
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