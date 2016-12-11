namespace Gu.Analyzers.Test.GU0031DisposeMemberTests
{
    using System.Threading;
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class CodeFixDisposeMember : CodeFixVerifier<GU0031DisposeMember, DisposeMemberCodeFixProvider>
    {
        [Test]
        public async Task NotDisposingPrivateReadonlyFieldInDisposeMethod()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    ↓private readonly Stream stream = File.OpenRead(string.Empty);

    public void Dispose()
    {
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private readonly Stream stream = File.OpenRead(string.Empty);

    public void Dispose()
    {
        this.stream.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingFieldAssignedInExpressionBody()
        {
            var disposableCode = @"
using System;
class Disposable : IDisposable {
    public void Dispose() { }
}";

            var testCode = @"
using System;
class Foo : IDisposable
{
    ↓IDisposable _disposable;
    public void Create()  => _disposable = new Disposable();
    public void Dispose()
    {
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(new[] { disposableCode, testCode }, expected).ConfigureAwait(false);

            var fixedCode = @"
using System;
class Foo : IDisposable
{
    IDisposable _disposable;
    public void Create()  => _disposable = new Disposable();
    public void Dispose()
    {
        _disposable?.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(new[] { disposableCode, testCode }, new[] { disposableCode, fixedCode }).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingPrivateFieldThatCanBeNullInDisposeMethod()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    ↓private Stream stream = File.OpenRead(string.Empty);

    public void Meh()
    {
        this.stream.Dispose();
        this.stream = null;
    }

    public void Dispose()
    {
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private Stream stream = File.OpenRead(string.Empty);

    public void Meh()
    {
        this.stream.Dispose();
        this.stream = null;
    }

    public void Dispose()
    {
        this.stream?.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingFieldAssignedInCtor()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    ↓private readonly Stream stream;

    public Foo()
    {
        this.stream = File.OpenRead(string.Empty);
    }

    public void Dispose()
    {
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private readonly Stream stream;

    public Foo()
    {
        this.stream = File.OpenRead(string.Empty);
    }

    public void Dispose()
    {
        this.stream.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingFieldAssignedInCtorNullCoalescing()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    ↓private readonly Stream stream;

    public Foo(Stream stream)
    {
        this.stream = stream ?? File.OpenRead(string.Empty);
    }

    public void Dispose()
    {
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private readonly Stream stream;

    public Foo(Stream stream)
    {
        this.stream = stream ?? File.OpenRead(string.Empty);
    }

    public void Dispose()
    {
        this.stream.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingFieldAssignedInCtorTernary()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    ↓private readonly Stream stream;

    public Foo(Stream stream)
    {
        this.stream = stream != null ? stream : File.OpenRead(string.Empty);
    }

    public void Dispose()
    {
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private readonly Stream stream;

    public Foo(Stream stream)
    {
        this.stream = stream != null ? stream : File.OpenRead(string.Empty);
    }

    public void Dispose()
    {
        this.stream.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingProtectedFieldInDisposeMethod()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    ↓protected Stream stream = File.OpenRead(string.Empty);
        
    public void Dispose()
    {
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    protected Stream stream = File.OpenRead(string.Empty);
        
    public void Dispose()
    {
        this.stream?.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingFieldInDisposeMethod2()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private readonly Stream stream1 = File.OpenRead(string.Empty);
    ↓private readonly Stream stream2 = File.OpenRead(string.Empty);
        
    public void Dispose()
    {
        stream1.Dispose();
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private readonly Stream stream1 = File.OpenRead(string.Empty);
    private readonly Stream stream2 = File.OpenRead(string.Empty);
        
    public void Dispose()
    {
        stream1.Dispose();
        this.stream2.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingFieldInDisposeMethodExpressionBody()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private readonly Stream stream1 = File.OpenRead(string.Empty);
    ↓private readonly Stream stream2 = File.OpenRead(string.Empty);
        
    public void Dispose() => this.stream1.Dispose();
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private readonly Stream stream1 = File.OpenRead(string.Empty);
    private readonly Stream stream2 = File.OpenRead(string.Empty);
        
    public void Dispose()
    {
        this.stream1.Dispose();
        this.stream2.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingFieldOfTypeObjectInDisposeMethod()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    ↓private readonly object stream = File.OpenRead(string.Empty);
        
    public void Dispose()
    {
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private readonly object stream = File.OpenRead(string.Empty);
        
    public void Dispose()
    {
        (this.stream as IDisposable)?.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingFieldWhenContainingTypeIsNotIDisposable()
        {
            var testCode = @"
    using System;
    using System.IO;

    public sealed class Foo
    {
        ↓private readonly Stream stream = File.OpenRead(string.Empty);
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
            await this.VerifyCSharpFixAsync(testCode, testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingPropertyWhenInitializedInline()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    ↓public Stream Stream { get; set; } = File.OpenRead(string.Empty);
        
    public void Dispose()
    {
    }
}";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    public Stream Stream { get; set; } = File.OpenRead(string.Empty);
        
    public void Dispose()
    {
        this.Stream?.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingGetOnlyPropertyWhenInitializedInline()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    ↓public Stream Stream { get; } = File.OpenRead(string.Empty);
        
    public void Dispose()
    {
    }
}";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    public Stream Stream { get; } = File.OpenRead(string.Empty);
        
    public void Dispose()
    {
        this.Stream.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingGetSetPropertyOfTypeObjectWhenInitializedInline()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    ↓public object Stream { get; set; } = File.OpenRead(string.Empty);
        
    public void Dispose()
    {
    }
}";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    public object Stream { get; set; } = File.OpenRead(string.Empty);
        
    public void Dispose()
    {
        (this.Stream as IDisposable)?.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingGetOnlyPropertyOfTypeObjectWhenInitializedInline()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    ↓public object Stream { get; } = File.OpenRead(string.Empty);
        
    public void Dispose()
    {
    }
}";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    public object Stream { get; } = File.OpenRead(string.Empty);
        
    public void Dispose()
    {
        (this.Stream as IDisposable)?.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingGetSetPropertyWhenInitializedInCtor()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    public Foo()
    {
        this.Stream = File.OpenRead(string.Empty);
    }

    ↓public Stream Stream { get; set; }

    public void Dispose()
    {
    }
}";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    public Foo()
    {
        this.Stream = File.OpenRead(string.Empty);
    }

    public Stream Stream { get; set; }

    public void Dispose()
    {
        this.Stream?.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingGetSetPropertyWithBackingFieldWhenInitializedInCtor()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    ↓private Stream _stream;

    public Foo()
    {
        this.Stream = File.OpenRead(string.Empty);
    }

    public Stream Stream
    {
        get { return _stream; }
        set { _stream = value; }
    }

    public void Dispose()
    {
    }
}";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private Stream _stream;

    public Foo()
    {
        this.Stream = File.OpenRead(string.Empty);
    }

    public Stream Stream
    {
        get { return _stream; }
        set { _stream = value; }
    }

    public void Dispose()
    {
        _stream?.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingGetOnlyPropertyWhenInitializedInCtor()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    public Foo()
    {
        this.Stream = File.OpenRead(string.Empty);
    }

    ↓public Stream Stream { get; }

    public void Dispose()
    {
    }
}";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    public Foo()
    {
        this.Stream = File.OpenRead(string.Empty);
    }

    public Stream Stream { get; }

    public void Dispose()
    {
        this.Stream.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}