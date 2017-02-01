namespace Gu.Analyzers.Test.GU0036DontDisposeInjectedTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<GU0036DontDisposeInjected>
    {
        [Test]
        public async Task InjectedInClassThatIsNotIDisposable()
        {
            var testCode = @"
    using System;

    public sealed class Foo
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedInClassThatIsIDisposable()
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
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedInClassThatIsIDisposableManyCtors()
        {
            var testCode = @"
using System;

public sealed class Foo : IDisposable
{
    private readonly IDisposable disposable;

    public Foo(IDisposable disposable)
        : this(disposable, ""meh"")
    {
    }

    public Foo(IDisposable disposable, IDisposable gah, int meh)
        : this(disposable, meh)
    {
    }

    private Foo(IDisposable disposable, int meh)
        : this(disposable, meh.ToString())
    {
    }

    private Foo(IDisposable disposable, string meh)
    {
        this.disposable = disposable;
    }

    public void Dispose()
    {
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedObjectInClassThatIsIDisposableWhenTouchingInjectedInDisposeMethod()
        {
            var testCode = @"
    using System;

    public sealed class Foo : IDisposable
    {
        private object meh;

        public Foo(object meh)
        {
            this.meh = meh;
        }

        public void Dispose()
        {
            meh = null;
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingFieldInVirtualDispose()
        {
            var testCode = @"
    using System;
    using System.IO;

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
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InjectingIntoPrivateCtor()
        {
            var disposableCode = @"
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }";

            var testCode = @"
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
            await this.VerifyHappyPathAsync(disposableCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoresWhenDisposingRecursiveProperty()
        {
            var testCode = @"
    using System;

    public class Foo : IDisposable
    {
        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
            this.RecursiveProperty.Dispose();
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoresWhenNotDisposingRecursiveProperty()
        {
            var testCode = @"
    using System;

    public class Foo : IDisposable
    {
        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoresWhenDisposingFieldAssignedWithRecursiveProperty()
        {
            var testCode = @"
    using System;

    public class Foo : IDisposable
    {
        private IDisposable disposable;

        public Foo()
        {
            this.disposable = this.RecursiveProperty;
        }

        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoresWhenNotDisposingFieldAssignedWithRecursiveProperty()
        {
            var testCode = @"
    using System;

    public class Foo : IDisposable
    {
        private IDisposable disposable;

        public Foo()
        {
            this.disposable = this.RecursiveProperty;
        }

        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoresWhenDisposingRecursiveMethod()
        {
            var testCode = @"
using System;

public class Foo
{
    public IDisposable RecursiveMethod() => RecursiveMethod();

    public void Dispose()
    {
        this.RecursiveMethod().Dispose();
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}