namespace Gu.Analyzers.Test.GU0036DontDisposeInjectedTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0036DontDisposeInjected>
    {
        [Test]
        public async Task NotIDisposable()
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
        public async Task NotDisposingField()
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
    }
}