namespace Gu.Analyzers.Test.Sandbox
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class Foo : IReadOnlyList<int>
    {
        public int Count { get; }

        public int this[int index]
        {
            get
            {
                if (index != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index, "message");
                }

                return A;
            }
        }

        public int A { get; set; }

        public IEnumerator<int> GetEnumerator()
        {
            yield return A;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
