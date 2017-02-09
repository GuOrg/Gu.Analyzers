namespace Gu.Analyzers.Test.GU0036DontDisposeInjectedTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class Diagnostics : DiagnosticVerifier<GU0036DontDisposeInjected>
    {
        [TestCase("this.disposable.Dispose();")]
        [TestCase("this.disposable?.Dispose();")]
        [TestCase("disposable.Dispose();")]
        [TestCase("disposable?.Dispose();")]
        public async Task DisposingField(string disposeCall)
        {
            var testCode = @"
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            ↓this.disposable.Dispose();
        }
    }";
            testCode = testCode.AssertReplace("this.disposable.Dispose();", disposeCall);

            var expected = this.CSharpDiagnostic()
                   .WithLocationIndicated(ref testCode)
                   .WithMessage("Don't dispose injected.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);
        }

        [TestCase("this.disposable.Dispose();")]
        [TestCase("this.disposable?.Dispose();")]
        [TestCase("disposable.Dispose();")]
        [TestCase("disposable?.Dispose();")]
        public async Task DisposingPublicField(string disposeCall)
        {
            var testCode = @"
    using System;

    public sealed class Foo : IDisposable
    {
        public IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            ↓this.disposable.Dispose();
        }
    }";
            testCode = testCode.AssertReplace("this.disposable.Dispose();", disposeCall);

            var expected = this.CSharpDiagnostic()
                   .WithLocationIndicated(ref testCode)
                   .WithMessage("Don't dispose injected.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DisposingPublicFieldOutsideOfLock()
        {
            var testCode = @"
namespace RoslynSandBox
{
    using System;

    public class Foo : IDisposable
    {
        private readonly object gate;

        public IDisposable disposable;
        private bool disposed;

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            var toDispose = (IDisposable)null;
            lock (this.gate)
            {
                if (this.disposed)
                {
                    return;
                }

                this.disposed = true;
                toDispose = this.disposable;
                this.disposable = null;
            }

            ↓toDispose?.Dispose();
        }
    }
}";

            var expected = this.CSharpDiagnostic()
                   .WithLocationIndicated(ref testCode)
                   .WithMessage("Don't dispose injected.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);
        }

        [TestCase("this.Disposable.Dispose();")]
        [TestCase("this.Disposable?.Dispose();")]
        [TestCase("Disposable.Dispose();")]
        [TestCase("Disposable?.Dispose();")]
        public async Task DisposingProperty(string disposeCall)
        {
            var testCode = @"
    using System;

    public sealed class Foo : IDisposable
    {
        public Foo(IDisposable disposable)
        {
            this.Disposable = disposable;
        }

        public IDisposable Disposable { get; }

        public void Dispose()
        {
            ↓this.Disposable.Dispose();
        }
    }";
            testCode = testCode.AssertReplace("this.Disposable.Dispose();", disposeCall);

            var expected = this.CSharpDiagnostic()
                   .WithLocationIndicated(ref testCode)
                   .WithMessage("Don't dispose injected.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);
        }

        [TestCase("this.Disposable.Dispose();")]
        [TestCase("this.Disposable?.Dispose();")]
        [TestCase("Disposable.Dispose();")]
        [TestCase("Disposable?.Dispose();")]
        public async Task DisposingCaclulatedPropertyStatementBody(string disposeCall)
        {
            var fooCode = @"
using System;

public sealed class Foo
{
    public Foo(IDisposable disposable)
    {
        this.Disposable = disposable;
    }

    public IDisposable Disposable { get; }
}";

            var testCode = @"
using System;

public sealed class Bar : IDisposable
{
    private readonly Foo foo;

    public Bar(Foo foo)
    {
        this.foo = foo;
    }

    public IDisposable Disposable
    {
        get
        {
           return this.foo.Disposable;
        }
    }

    public void Dispose()
    {
        ↓this.Disposable.Dispose();
    }
}";

            testCode = testCode.AssertReplace("this.Disposable.Dispose();", disposeCall);

            var expected = this.CSharpDiagnostic()
                   .WithLocationIndicated(ref testCode)
                   .WithMessage("Don't dispose injected.");
            await this.VerifyCSharpDiagnosticAsync(new[] { fooCode, testCode }, expected)
                      .ConfigureAwait(false);
        }

        [TestCase("this.Disposable.Dispose();")]
        [TestCase("this.Disposable?.Dispose();")]
        [TestCase("Disposable.Dispose();")]
        [TestCase("Disposable?.Dispose();")]
        public async Task DisposingCaclulatedPropertyExpressionBody(string disposeCall)
        {
            var fooCode = @"
using System;

public sealed class Foo
{
    public Foo(IDisposable disposable)
    {
        this.Disposable = disposable;
    }

    public IDisposable Disposable { get; }
}";

            var testCode = @"
using System;

public sealed class Bar : IDisposable
{
    private readonly Foo foo;

    public Bar(Foo foo)
    {
        this.foo = foo;
    }

    public IDisposable Disposable => this.foo.Disposable;

    public void Dispose()
    {
        ↓this.Disposable.Dispose();
    }
}";

            testCode = testCode.AssertReplace("this.Disposable.Dispose();", disposeCall);

            var expected = this.CSharpDiagnostic()
                   .WithLocationIndicated(ref testCode)
                   .WithMessage("Don't dispose injected.");
            await this.VerifyCSharpDiagnosticAsync(new[] { fooCode, testCode }, expected)
                      .ConfigureAwait(false);
        }

        [TestCase("this.Disposable.Dispose();")]
        [TestCase("this.Disposable?.Dispose();")]
        [TestCase("Disposable.Dispose();")]
        [TestCase("Disposable?.Dispose();")]
        public async Task DisposingMutableProperty(string disposeCall)
        {
            var testCode = @"
    using System;

    public sealed class Foo : IDisposable
    {
        public IDisposable Disposable { get; set; }

        public void Dispose()
        {
            ↓this.Disposable.Dispose();
        }
    }";
            testCode = testCode.AssertReplace("this.Disposable.Dispose();", disposeCall);

            var expected = this.CSharpDiagnostic()
                   .WithLocationIndicated(ref testCode)
                   .WithMessage("Don't dispose injected.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DisposingCtorParameter()
        {
            var testCode = @"
using System;

public class Foo
{
    public Foo(IDisposable meh)
    {
        ↓meh.Dispose();
    }
}";

            var expected = this.CSharpDiagnostic()
                   .WithLocationIndicated(ref testCode)
                   .WithMessage("Don't dispose injected.");
            await this.VerifyCSharpDiagnosticAsync(new[] { testCode }, expected)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DisposingParameter()
        {
            var testCode = @"
    using System;

    public class Foo
    {
        public void Bar(IDisposable meh)
        {
            ↓meh.Dispose();
        }
    }";

            var expected = this.CSharpDiagnostic()
                   .WithLocationIndicated(ref testCode)
                   .WithMessage("Don't dispose injected.");
            await this.VerifyCSharpDiagnosticAsync(new[] { testCode }, expected)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedViaMethod()
        {
            var testCode = @"
using System;

public sealed class Foo : IDisposable
{
    private IDisposable disposable;

    public void Meh(IDisposable disposable)
    {
        this.disposable = disposable;
    }

    public void Dispose()
    {
        ↓this.disposable.Dispose();
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't dispose injected.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task DisposingFieldInVirtualDispose()
        {
            var testCode = @"
    using System;

    public class Foo : IDisposable
    {
        private readonly IDisposable disposable;
        private bool disposed;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ↓this.disposable.Dispose();
            }
        }

        protected void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }";
            var expected = this.CSharpDiagnostic()
                   .WithLocationIndicated(ref testCode)
                   .WithMessage("Don't dispose injected.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task UsingField1()
        {
            var testCode = @"
using System;

public class Foo
{
    private readonly IDisposable disposable;

    public Foo(IDisposable disposable)
    {
        this.disposable = disposable;
        using (↓disposable)
        {
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                   .WithLocationIndicated(ref testCode)
                   .WithMessage("Don't dispose injected.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task UsingField2()
        {
            var testCode = @"
using System;

public class Foo
{
    private readonly IDisposable disposable;

    public Foo(IDisposable disposable)
    {
        this.disposable = disposable;
        using (var meh = ↓disposable)
        {
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                   .WithLocationIndicated(ref testCode)
                   .WithMessage("Don't dispose injected.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);
        }

        [TestCase("this.disposable.Dispose();")]
        [TestCase("this.disposable?.Dispose();")]
        [TestCase("disposable.Dispose();")]
        [TestCase("disposable?.Dispose();")]
        [TestCase("this.disposable.Disposable.Dispose();")]
        [TestCase("this.disposable?.Disposable.Dispose();")]
        [TestCase("this.disposable?.Disposable?.Dispose();")]
        [TestCase("disposable.Disposable.Dispose();")]
        [TestCase("disposable?.Disposable.Dispose();")]
        [TestCase("disposable?.Disposable?.Dispose();")]
        public async Task InjectedSingleAssignmentDisposable(string dispose)
        {
            var testCode = @"
namespace Gu.Reactive
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public abstract class Foo : IDisposable
    {
        private readonly SingleAssignmentDisposable disposable;

        protected Foo(SingleAssignmentDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            ↓this.disposable.Dispose();
        }
     }
}";
            testCode = testCode.AssertReplace("this.disposable.Dispose();", dispose);
            var expected = this.CSharpDiagnostic()
                   .WithLocationIndicated(ref testCode)
                   .WithMessage("Don't dispose injected.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);
        }

        [Explicit("Don't think this is very useful.")]
        [TestCase("action(disposable)")]
        [TestCase("action.Invoke(disposable)")]
        public async Task IgnoreLambdaUsageOnInjected(string disposeCode)
        {
            var testCode = @"
namespace RoslynSandBox
{
    using System;

    public class Foo
    {
        public Foo(IDisposable disposable)
        {
            Action<IDisposable> action = x => x.Dispose();
            ↓action(disposable);
        }
    }
}";
            testCode = testCode.AssertReplace("action(disposable)", disposeCode);
            var expected = this.CSharpDiagnostic()
                   .WithLocationIndicated(ref testCode)
                   .WithMessage("Don't dispose injected.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);
        }
    }
}