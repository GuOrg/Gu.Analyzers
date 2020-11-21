// ReSharper disable All
namespace ValidCode
{
    using System;
    using System.Reactive.Disposables;

    internal static class CompositeDisposableExt
    {
        internal static T AddAndReturn<T>(this CompositeDisposable disposable, T item)
            where T : IDisposable
        {
            if (item != null)
            {
                disposable.Add(item);
            }

#pragma warning disable CS8603 // Possible null reference return.
            return item;
#pragma warning restore CS8603 // Possible null reference return.
        }
    }
}
