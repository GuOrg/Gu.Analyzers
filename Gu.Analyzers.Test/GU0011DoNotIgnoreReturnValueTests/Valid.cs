namespace Gu.Analyzers.Test.GU0011DoNotIgnoreReturnValueTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static partial class Valid
    {
        private static readonly GU0011DoNotIgnoreReturnValue Analyzer = new GU0011DoNotIgnoreReturnValue();

        [Test]
        public static void ChainedCtor()
        {
            var code = @"
namespace N
{
    using System.Text;

    public class C
    {
        public C()
            : this(new StringBuilder())
        {
        }

        private C(StringBuilder builder)
        {
            this.Builder = builder;
        }

        public StringBuilder Builder { get; }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void RealisticClass()
        {
            var code = @"
namespace N
{
    public class C
    {
        public int Value { get; set; }
    
        private void M1()
        {
            M2();
        }

        private void M2()
        {
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void Using()
        {
            var code = @"
namespace N
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;


    public static class C
    {
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            return await task.ConfigureAwait(false);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void RealisticExtensionMethodClass()
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    internal static class EnumerableExt
    {
        internal static bool TryElementAt<TCollection, TItem>(this TCollection source, int index, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            result = default(TItem);
            if (source is null)
            {
                return false;
            }

            if (source.Count <= index)
            {
                return false;
            }

            result = source[index];
            return true;
        }

        internal static bool TrySingle<TCollection, TItem>(this TCollection source, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            if (source.Count == 1)
            {
                result = source[0];
                return true;
            }

            result = default(TItem);
            return false;
        }

        internal static bool TrySingle<TCollection, TItem>(this TCollection source, Func<TItem, bool> selector, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(TItem);
            return false;
        }

        internal static bool TryFirst<TCollection, TItem>(this TCollection source, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            if (source.Count == 0)
            {
                result = default(TItem);
                return false;
            }

            result = source[0];
            return true;
        }

        internal static bool TryFirst<TCollection, TItem>(this TCollection source, Func<TItem, bool> selector, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(TItem);
            return false;
        }

        internal static bool TryLast<TCollection, TItem>(this TCollection source, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            if (source.Count == 0)
            {
                result = default(TItem);
                return false;
            }

            result = source[source.Count - 1];
            return true;
        }

        internal static bool TryLast<TCollection, TItem>(this TCollection source, Func<TItem, bool> selector, out TItem result)
             where TCollection : IReadOnlyList<TItem>
        {
            for (var i = source.Count - 1; i >= 0; i--)
            {
                var item = source[i];
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(TItem);
            return false;
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void VoidMethod()
        {
            var code = @"
namespace N
{
    public class C
    {
        private void M1()
        {
            M2();
        }

        private void M2()
        {
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void VoidMethodWithReturn()
        {
            var code = @"
namespace N
{
    public class C
    {
        private void M1()
        {
            M2();
        }

        private void M2()
        {
            return;
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void StaticVoidMethod()
        {
            var code = @"
namespace N
{
    public class C
    {
        private void M1()
        {
            M2();
        }

        private static void M2()
        {
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void StaticVoidMethodWithReturn()
        {
            var code = @"
namespace N
{
    public class C
    {
        private void M1()
        {
            M2();
        }

        private static void M2()
        {
            return;
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IfTry()
        {
            var code = @"
namespace N
{
    public class C
    {
        private void M()
        {
            int value;
            if(Try(out value))
            {
            }
        }

        private bool Try(out int value)
        {
            value = 1;
            return true;
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenThrowing()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public C M()
        {
            throw new Exception();
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenInvocationInExpressionBody()
        {
            var code = @"
namespace N
{
    public class C
    {
        public C M1()
        {
            return this;
        }

        public void M2() => M1();
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenNewInExpressionBody()
        {
            var code = @"
namespace N
{
    public class C
    {
        public void M() => new C();
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
