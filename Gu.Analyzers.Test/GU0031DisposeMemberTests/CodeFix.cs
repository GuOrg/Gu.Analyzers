namespace Gu.Analyzers.Test.GU0031DisposeMemberTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class CodeFix : CodeFixVerifier<GU0031DisposeMember, DisposeMemberCodeFixProvider>
    {
        private static readonly string DisposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";

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
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

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
            await this.VerifyCSharpDiagnosticAsync(new[] { DisposableCode, testCode }, expected).ConfigureAwait(false);

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
            await this.VerifyCSharpFixAsync(new[] { DisposableCode, testCode }, new[] { DisposableCode, fixedCode }).ConfigureAwait(false);
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
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

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
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

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
        [Explicit("Not fixing this.")]
        public async Task NotDisposingFieldWhenConditionallyAssignedInCtor()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    ↓private readonly Stream stream;

    public Foo(bool condition)
    {
        if(condition)
        {
            this.stream = File.OpenRead(string.Empty);
        }
    }

    public void Dispose()
    {
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private readonly Stream stream;

    public Foo(bool condition)
    {
        if(condition)
        {
            this.stream = File.OpenRead(string.Empty);
        }
    }

    public void Dispose()
    {
        this.stream?.Dispose();
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
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

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
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

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
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
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
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

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
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

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
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

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
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

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
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

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
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

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
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

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
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

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
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

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
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

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

        [Test]
        public async Task DisposeMemberWhenVirtualDisposeMethod()
        {
            var testCode = @"
using System;
using System.IO;

public class Foo : IDisposable
{
    ↓private readonly Stream stream = File.OpenRead(string.Empty);
    private bool disposed;

    public void Dispose()
    {
        this.Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        if (disposing)
        {
        }
    }

    protected void ThrowIfDisposed()
    {
        if (this.disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }
}";
            var expected = this.CSharpDiagnostic(GU0031DisposeMember.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public class Foo : IDisposable
{
    private readonly Stream stream = File.OpenRead(string.Empty);
    private bool disposed;

    public void Dispose()
    {
        this.Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        if (disposing)
        {
            this.stream.Dispose();
        }
    }

    protected void ThrowIfDisposed()
    {
        if (this.disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, codeFixIndex: 0)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DisposeMemberWhenOverriddenDisposeMethod()
        {
            var baseCode = @"
using System;

public class BaseClass : IDisposable
{
    private bool disposed;

    public void Dispose()
    {
        this.Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        if (disposing)
        {
        }
    }

    protected void ThrowIfDisposed()
    {
        if (this.disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }
}";
            var testCode = @"
using System.IO;

public class Foo : BaseClass
{
    ↓private readonly Stream stream = File.OpenRead(string.Empty);
    private bool disposed;

    protected override void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        if (disposing)
        {
        }

        base.Dispose(disposing);
    }
}";
            var expected = this.CSharpDiagnostic(GU0031DisposeMember.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(new[] { baseCode, testCode }, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"
using System.IO;

public class Foo : BaseClass
{
    private readonly Stream stream = File.OpenRead(string.Empty);
    private bool disposed;

    protected override void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        if (disposing)
        {
            this.stream.Dispose();
        }

        base.Dispose(disposing);
    }
}";
            await this.VerifyCSharpFixAsync(new[] { baseCode, testCode }, new[] { baseCode, fixedCode }, allowNewCompilerDiagnostics: true, codeFixIndex: 0)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task CtorPassingCreatedIntoPrivateCtor()
        {
            var testCode = @"
using System;

public sealed class Foo : IDisposable
{
    ↓private readonly IDisposable disposable;

    public Foo()
        : this(new Disposable())
    {
    }

    private Foo(IDisposable disposable)
    {
        this.disposable = disposable;
    }

    public void Dispose()
    {
    }
}";
            var expected = this.CSharpDiagnostic(GU0031DisposeMember.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(new[] { DisposableCode, testCode }, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"
using System;

public sealed class Foo : IDisposable
{
    private readonly IDisposable disposable;

    public Foo()
        : this(new Disposable())
    {
    }

    private Foo(IDisposable disposable)
    {
        this.disposable = disposable;
    }

    public void Dispose()
    {
        this.disposable.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(new[] { DisposableCode, testCode }, new[] { DisposableCode, fixedCode })
          .ConfigureAwait(false);
        }

        [Test]
        public async Task FactoryPassingCreatedIntoPrivateCtor()
        {
            var testCode = @"
using System;

public sealed class Foo : IDisposable
{
    ↓private readonly IDisposable disposable;

    private Foo(IDisposable disposable)
    {
        this.disposable = disposable;
    }

    public static Foo Create()
    {
        return new Foo(new Disposable());
    }

    public void Dispose()
    {
    }
}";
            var expected = this.CSharpDiagnostic(GU0031DisposeMember.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(new[] { DisposableCode, testCode }, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"
using System;

public sealed class Foo : IDisposable
{
    private readonly IDisposable disposable;

    private Foo(IDisposable disposable)
    {
        this.disposable = disposable;
    }

    public static Foo Create()
    {
        return new Foo(new Disposable());
    }

    public void Dispose()
    {
        this.disposable.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(new[] { DisposableCode, testCode }, new[] { DisposableCode, fixedCode })
          .ConfigureAwait(false);
        }

        [Test]
        public async Task ExtensionMethodFactoryAssigningInCtor()
        {
            var factoryCode = @"
using System;

public static class Factory
{
    public static IDisposable AsDisposable(this object value)
    {
        return new Disposable();
    }
}";

            var testCode = @"
using System;

public sealed class Foo : IDisposable
{
    ↓private readonly IDisposable disposable;

    private Foo(object value)
    {
        this.disposable = value.AsDisposable();
    }

    public void Dispose()
    {
    }
}";
            var expected = this.CSharpDiagnostic(GU0031DisposeMember.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(new[] { DisposableCode, factoryCode, testCode }, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"
using System;

public sealed class Foo : IDisposable
{
    private readonly IDisposable disposable;

    private Foo(object value)
    {
        this.disposable = value.AsDisposable();
    }

    public void Dispose()
    {
        this.disposable.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(new[] { DisposableCode, factoryCode, testCode }, new[] { DisposableCode, factoryCode, fixedCode })
          .ConfigureAwait(false);
        }

        [Test]
        public async Task GenericExtensionMethodFactoryAssigningInCtor()
        {
            var factoryCode = @"
using System;

public static class Factory
{
    public static IDisposable AsDisposable<T>(this T value)
    {
        return new Disposable();
    }
}";

            var testCode = @"
using System;

public sealed class Foo : IDisposable
{
    ↓private readonly IDisposable disposable;

    private Foo(object value)
    {
        this.disposable = value.AsDisposable();
    }

    public void Dispose()
    {
    }
}";
            var expected = this.CSharpDiagnostic(GU0031DisposeMember.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(new[] { DisposableCode, factoryCode, testCode }, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"
using System;

public sealed class Foo : IDisposable
{
    private readonly IDisposable disposable;

    private Foo(object value)
    {
        this.disposable = value.AsDisposable();
    }

    public void Dispose()
    {
        this.disposable.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(new[] { DisposableCode, factoryCode, testCode }, new[] { DisposableCode, factoryCode, fixedCode })
          .ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingPrivateReadonlyFieldOfTypeSubclassInDisposeMethod()
        {
            var subclassCode = @"
public sealed class Bar : Disposable
{
}";
            var testCode = @"
using System;

public sealed class Foo : IDisposable
{
    ↓private readonly Bar bar = new Bar();

    public void Dispose()
    {
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(new[] { DisposableCode, subclassCode, testCode }, expected).ConfigureAwait(false);

            var fixedCode = @"
using System;

public sealed class Foo : IDisposable
{
    private readonly Bar bar = new Bar();

    public void Dispose()
    {
        this.bar.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(new[] { DisposableCode, subclassCode, testCode }, new[] { DisposableCode, subclassCode, fixedCode }).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingPrivateReadonlyFieldOfTypeSubclassGenericInDisposeMethod()
        {
            var subclassCode = @"
public sealed class Bar<T> : Disposable
{
}";
            var testCode = @"
using System;

public sealed class Foo<T> : IDisposable
{
    ↓private readonly Bar<T> bar = new Bar<T>();

    public void Dispose()
    {
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(new[] { DisposableCode, subclassCode, testCode }, expected).ConfigureAwait(false);

            var fixedCode = @"
using System;

public sealed class Foo<T> : IDisposable
{
    private readonly Bar<T> bar = new Bar<T>();

    public void Dispose()
    {
        this.bar.Dispose();
    }
}";
            await this.VerifyCSharpFixAsync(new[] { DisposableCode, subclassCode, testCode }, new[] { DisposableCode, subclassCode, fixedCode }).ConfigureAwait(false);
        }
    }
}