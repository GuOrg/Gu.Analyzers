namespace Gu.Analyzers.Test.GU0035ImplementIDisposableTests
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    using NUnit.Framework;

    internal class CodeFixImplementIDisposable : CodeFixVerifier<GU0035ImplementIDisposable, ImplementIDisposableCodeFixProvider>
    {
        [Test]
        public async Task ImplementIDisposable0()
        {
            var testCode = @"
using System.IO;

public class Foo
{
    ↓private readonly Stream stream = File.OpenRead(string.Empty);
}";
            var expected = this.CSharpDiagnostic(GU0035ImplementIDisposable.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Implement IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private readonly Stream stream = File.OpenRead(string.Empty);
    private bool disposed;

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (this.disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, codeFixIndex: 0, numberOfFixAllIterations: 2)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposable1()
        {
            var testCode = @"
using System.IO;

public class Foo
{
    ↓private readonly Stream stream = File.OpenRead(string.Empty);
}";
            var expected = this.CSharpDiagnostic(GU0035ImplementIDisposable.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Implement IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"using System;
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
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, codeFixIndex: 1, numberOfFixAllIterations: 2)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposableSealedClass()
        {
            var testCode = @"
using System.IO;

public sealed class Foo
{
    ↓private readonly Stream stream = File.OpenRead(string.Empty);
}";
            var expected = this.CSharpDiagnostic(GU0035ImplementIDisposable.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Implement IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private readonly Stream stream = File.OpenRead(string.Empty);
    private bool disposed;

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (this.disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, numberOfFixAllIterations: 2)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposableAbstractClass()
        {
            var testCode = @"
using System.IO;

public abstract class Foo
{
    ↓private readonly Stream stream = File.OpenRead(string.Empty);
}";
            var expected = this.CSharpDiagnostic(GU0035ImplementIDisposable.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Implement IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"using System;
using System.IO;

public abstract class Foo : IDisposable
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
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, numberOfFixAllIterations: 2)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposableWhenInterfaceIsMissing()
        {
            var testCode = @"
using System.IO;

public class Foo
{
    ↓private readonly Stream stream = File.OpenRead(string.Empty);

    public void Dispose()
    {
    }
}";
            var expected = this.CSharpDiagnostic(GU0035ImplementIDisposable.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Implement IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private readonly Stream stream = File.OpenRead(string.Empty);

    public void Dispose()
    {
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, codeFixIndex: 0, numberOfFixAllIterations: 2)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposableDisposeMethod()
        {
            var testCode = @"
using System;

public class Foo : ↓IDisposable
{
}";
            var expected = this.CSharpDiagnostic("CS0535")
                               .WithLocationIndicated(ref testCode);
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"
using System;

public sealed class Foo : IDisposable
{
    private bool disposed;

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (this.disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposableDisposeMethodWithProtectedPrivateSetProperty()
        {
            var testCode = @"
using System;

public class Foo : ↓IDisposable
{
    protected int Value { get; private set; }
}";
            var expected = this.CSharpDiagnostic("CS0535")
                               .WithLocationIndicated(ref testCode);
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"
using System;

public sealed class Foo : IDisposable
{
    private bool disposed;

    private int Value { get; set; }

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (this.disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposableDisposeMethodWithPublicVirtualMethod()
        {
            var testCode = @"
using System;

public class Foo : ↓IDisposable
{
    public virtual void Bar()
    {
    }
}";
            var expected = this.CSharpDiagnostic("CS0535")
                               .WithLocationIndicated(ref testCode);
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"
using System;

public sealed class Foo : IDisposable
{
    private bool disposed;

    public void Bar()
    {
    }

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (this.disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, codeFixIndex: 0, numberOfFixAllIterations: 2)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposableDisposeMethodWithProtectedVirtualMethod()
        {
            var testCode = @"
using System;

public class Foo : ↓IDisposable
{
    protected virtual void Bar()
    {
    }
}";
            var expected = this.CSharpDiagnostic("CS0535")
                               .WithLocationIndicated(ref testCode);
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"
using System;

public sealed class Foo : IDisposable
{
    private bool disposed;

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
    }

    private void Bar()
    {
    }

    private void ThrowIfDisposed()
    {
        if (this.disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, codeFixIndex: 0, numberOfFixAllIterations: 2)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task OverrideDispose()
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
}";
            var expected = this.CSharpDiagnostic(GU0035ImplementIDisposable.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Implement IDisposable.");
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
        }

        base.Dispose(disposing);
    }
}";
            await this.VerifyCSharpFixAsync(new[] { baseCode, testCode }, new[] { baseCode, fixedCode }, allowNewCompilerDiagnostics: true)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task VirtualDispose()
        {
            var testCode = @"
using System;

public class Foo : ↓IDisposable
{
}";
            var expected = this.CSharpDiagnostic("CS0535")
                               .WithLocationIndicated(ref testCode);
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"
using System;

public class Foo : IDisposable
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
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true, codeFixIndex: 1)
                      .ConfigureAwait(false);
        }

        internal override IEnumerable<DiagnosticAnalyzer> GetCSharpDiagnosticAnalyzers()
        {
            return base.GetCSharpDiagnosticAnalyzers()
                       .Concat(new[] { DummyAnalyzer.Default });
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class DummyAnalyzer : DiagnosticAnalyzer
        {
            public static readonly DummyAnalyzer Default = new DummyAnalyzer();

            private DummyAnalyzer()
            {
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
                ImmutableArray.Create(
                    new DiagnosticDescriptor(
                        id: "CS0535",
                        title: string.Empty,
                        messageFormat: "'Foo' does not implement interface member 'IDisposable.Dispose()'",
                        category: string.Empty,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: false));

            public override void Initialize(AnalysisContext context)
            {
            }
        }
    }
}