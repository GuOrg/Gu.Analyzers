namespace Gu.Analyzers.Test.GU0011DontIgnoreReturnValueTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class HappyPath
    {
        private static readonly GU0011DontIgnoreReturnValue Analyzer = new GU0011DontIgnoreReturnValue();

        [Test]
        public void ChainedCtor()
        {
            var testCode = @"
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void RealisticClass()
        {
            var testCode = @"
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

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void Using()
        {
            var testCode = @"
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void RealisticExtensionMethodClass()
        {
            var testCode = @"
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

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void VoidMethod()
        {
            var testCode = @"
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

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void VoidMethodWithReturn()
        {
            var testCode = @"
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

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void StaticVoidMethod()
        {
            var testCode = @"
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

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void StaticVoidMethodWithReturn()
        {
            var testCode = @"
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

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IfTry()
        {
            var testCode = @"
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

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenThrowing()
        {
            var testCode = @"
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenInvocationInExpressionBody()
        {
            var testCode = @"
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenNewInExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public void Meh() => new Foo();
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}