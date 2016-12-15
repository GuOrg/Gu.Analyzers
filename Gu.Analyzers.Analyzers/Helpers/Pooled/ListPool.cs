﻿namespace Gu.Analyzers
{
    using System.Collections.Generic;

    internal class ListPool<T>
    {
        private static readonly Pool<List<T>> Pool = new Pool<List<T>>(() => new List<T>(), x => x.Clear());

        public static Pool<List<T>>.Pooled Create()
        {
            return Pool.GetOrCreate();
        }
    }
}