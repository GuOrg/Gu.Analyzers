namespace Gu.Analyzers
{
    using System.Collections.Generic;

    internal static class ListExt
    {
        internal static bool AddIfNotExits<T>(this List<T> list, T item)
        {
            if (list == null ||
                item == null)
            {
                return false;
            }

            if (list.Contains(item))
            {
                return false;
            }

            list.Add(item);
            return true;
        }
    }
}