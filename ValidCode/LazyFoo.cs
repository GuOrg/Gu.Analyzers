// ReSharper disable All
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
