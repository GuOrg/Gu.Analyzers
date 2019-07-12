namespace Gu.Analyzers.Test.GU0011DontIgnoreReturnValueTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static partial class ValidCode
    {
        private static readonly GU0011DoNotIgnoreReturnValue Analyzer = new GU0011DoNotIgnoreReturnValue();

        [Test]
        public static void ChainedCtor()
        {
            var code = @"
namespace RoslynSandbox
{
    using System.Text;

    public class Foo
    {
        public Foo()
            : this(new StringBuilder())
        {
        }

        private Foo(StringBuilder builder)
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
namespace RoslynSandbox
{
    public class Foo
    {
        public int Value { get; set; }
    
        private void Bar()
        {
            Meh();
        }

        private void Meh()
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
namespace RoslynSandbox
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;


    public static class Foo
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
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    internal static class EnumerableExt
    {
        internal static bool TryElementAt<TCollection, TItem>(this TCollection source, int index, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            result = default(TItem);
            if (source == null)
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
namespace RoslynSandbox
{
    public class Foo
    {
        private void Bar()
        {
            Meh();
        }

        private void Meh()
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
namespace RoslynSandbox
{
    public class Foo
    {
        private void Bar()
        {
            Meh();
        }

        private void Meh()
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
namespace RoslynSandbox
{
    public class Foo
    {
        private void Bar()
        {
            Meh();
        }

        private static void Meh()
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
namespace RoslynSandbox
{
    public class Foo
    {
        private void Bar()
        {
            Meh();
        }

        private static void Meh()
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
namespace RoslynSandbox
{
    public class Foo
    {
        private void Bar()
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
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo Bar()
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
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo Bar()
        {
            return this;
        }

        public void Meh() => Bar();
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenNewInExpressionBody()
        {
            var code = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public void Meh() => new Foo();
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
