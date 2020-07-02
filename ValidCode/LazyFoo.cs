// ReSharper disable All
#pragma warning disable 1717
#pragma warning disable GU0011 // Don't ignore the return value.
#pragma warning disable GU0010 // Assigning same value.
#pragma warning disable IDE0009 // Member access should be qualified.
namespace ValidCode
{
    using System;

    internal sealed class LazyFoo : IDisposable
    {
        private readonly IDisposable? created;
        private bool disposed;
        private IDisposable? lazyDisposable;

        internal LazyFoo(IDisposable injected)
        {
            this.Disposable = injected ?? (this.created = new Disposable());
        }

        internal IDisposable Disposable { get; }

        internal IDisposable LazyDisposable => this.lazyDisposable ?? (this.lazyDisposable = new Disposable());

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.created?.Dispose();
            this.lazyDisposable?.Dispose();
        }
    }
}
