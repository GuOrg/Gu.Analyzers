namespace Gu.Analyzers
{
    using System;
    using System.Collections.Concurrent;

    internal static class ConcurrentQueueExt
    {
        internal static T GetOrCreate<T>(this ConcurrentQueue<T> queue)
            where T : new()
        {
            T item;
            if (queue.TryDequeue(out item))
            {
                return item;
            }

            return new T();
        }

        internal static T GetOrCreate<T>(this ConcurrentQueue<T> queue, Func<T> create)
        {
            T item;
            if (queue.TryDequeue(out item))
            {
                return item;
            }

            return create();
        }
    }
}