// ReSharper disable All
namespace ValidCode
{
    using System;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using Moq;

    internal abstract class UsingReactive : IDisposable
    {
        private readonly IDisposable subscription;
        private readonly SingleAssignmentDisposable singleAssignmentDisposable = new SingleAssignmentDisposable();

        internal UsingReactive(int no)
            : this(Create(no))
        {
        }

        internal UsingReactive(IObservable<object> observable)
        {
            this.subscription = observable.Subscribe(_ => { });
            this.singleAssignmentDisposable.Disposable = observable.Subscribe(_ => { });
        }

        public void Dispose()
        {
            this.subscription.Dispose();
            this.singleAssignmentDisposable.Dispose();
        }

        internal static async Task SleepAsync(IScheduler scheduler, TimeSpan dueTime)
        {
            await scheduler.Sleep(dueTime).ConfigureAwait(false);
        }

        private static IObservable<object> Create(int i)
        {
            return Observable.Empty<object>();
        }
    }
}

