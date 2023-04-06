// ReSharper disable All
namespace ValidCode
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Reactive.Disposables;

    internal class Foo1 : IDisposable, INotifyPropertyChanged
    {
        private static readonly PropertyChangedEventArgs IsDirtyPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsDirty));
        private readonly SingleAssignmentDisposable subscription = new SingleAssignmentDisposable();
        private readonly CompositeDisposable compositeDisposable = new CompositeDisposable();
        private readonly Lazy<IDisposable> lazyDisposable;
        private readonly IDisposable disposable;

        private bool isDirty;

        internal Foo1(IDisposable disposable)
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

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { this.PropertyChangedCore += value; }
            remove { this.PropertyChangedCore -= value; }
        }

        private event PropertyChangedEventHandler? PropertyChangedCore;

        internal IDisposable? Disposable => this.subscription.Disposable;

#pragma warning disable GU0021 // Calculated property allocates reference type
        internal IDisposable CreateDisposableProperty => new Disposable();
#pragma warning restore GU0021 // Calculated property allocates reference type

        internal string Text => this.AddAndReturnToString();

        internal bool IsDirty
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

        internal IDisposable CreateDisposable() => new Disposable();

        internal string AddAndReturnToString()
        {
            return this.compositeDisposable.AddAndReturn(new Disposable()).ToString()!;
        }

        private static IDisposable Bar(IDisposable disposable, IEnumerable<IDisposable>? disposables = null)
        {
            if (disposables is null)
            {
                return Bar(disposable, new[] { disposable });
            }

            return disposable;
        }
    }
}
