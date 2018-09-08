// ReSharper disable All
namespace ValidCode
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Reactive.Disposables;

    internal class Foo1 : IDisposable
    {
        private static readonly PropertyChangedEventArgs IsDirtyPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsDirty));
        private readonly SingleAssignmentDisposable subscription = new SingleAssignmentDisposable();
        private readonly CompositeDisposable compositeDisposable = new CompositeDisposable();
        private readonly Lazy<IDisposable> lazyDisposable;
        private readonly IDisposable disposable;

        private bool isDirty;

        public Foo1(IDisposable disposable)
        {
            this.subscription.Disposable = File.OpenRead(string.Empty);
            this.disposable = Bar(disposable);
            using (var temp = this.CreateDisposableProperty)
            {
            }

            using (var temp = this.CreateDisposable())
            {
            }

            this.lazyDisposable = new Lazy<IDisposable>(() =>
            {
                var temp = new Disposable();
                return temp;
            });
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { this.PropertyChangedCore += value; }
            remove { this.PropertyChangedCore -= value; }
        }

        private event PropertyChangedEventHandler PropertyChangedCore;

        public IDisposable Disposable => this.subscription.Disposable;

#pragma warning disable IDISP012 // Property should not return created disposable.
#pragma warning disable GU0021 // Calculated property allocates reference type.
        public IDisposable CreateDisposableProperty => new Disposable();
#pragma warning restore GU0021 // Calculated property allocates reference type.
#pragma warning restore IDISP012 // Property should not return created disposable.

        public string Text => this.AddAndReturnToString();

        public bool IsDirty
        {
            get
            {
                return this.isDirty;
            }

            private set
            {
                if (value == this.isDirty)
                {
                    return;
                }

                this.isDirty = value;
                this.PropertyChangedCore?.Invoke(this, IsDirtyPropertyChangedEventArgs);
            }
        }

        public void Dispose()
        {
            this.subscription.Dispose();
            this.compositeDisposable.Dispose();
            if (this.lazyDisposable.IsValueCreated)
            {
                this.lazyDisposable.Value.Dispose();
            }
        }

        public IDisposable CreateDisposable() => new Disposable();

        internal string AddAndReturnToString()
        {
            return this.compositeDisposable.AddAndReturn(new Disposable()).ToString();
        }

        private static IDisposable Bar(IDisposable disposable, IEnumerable<IDisposable> disposables = null)
        {
            if (disposables == null)
            {
                return Bar(disposable, new[] { disposable });
            }

            return disposable;
        }
    }
}
