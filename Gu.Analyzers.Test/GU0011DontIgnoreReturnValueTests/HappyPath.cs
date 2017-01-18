namespace Gu.Analyzers.Test.GU0011DontIgnoreReturnValueTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0011DontIgnoreReturnValue>
    {
        [Test]
        public async Task RealisticClass()
        {
            var testCode = @"
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
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task Using()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task RealisticExtensionMethodClass()
        {
            var testCode = @"
    using System;
    using System.Collections.Generic;

    internal static class EnumerableExt
    {
        internal static bool TryGetAtIndex<TCollection, TItem>(this TCollection source, int index, out TItem result)
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

        internal static bool TryGetSingle<TCollection, TItem>(this TCollection source, out TItem result)
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

        internal static bool TryGetSingle<TCollection, TItem>(this TCollection source, Func<TItem, bool> selector, out TItem result)
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

        internal static bool TryGetFirst<TCollection, TItem>(this TCollection source, out TItem result)
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

        internal static bool TryGetFirst<TCollection, TItem>(this TCollection source, Func<TItem, bool> selector, out TItem result)
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

        internal static bool TryGetLast<TCollection, TItem>(this TCollection source, out TItem result)
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

        internal static bool TryGetLast<TCollection, TItem>(this TCollection source, Func<TItem, bool> selector, out TItem result)
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
    }";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task VoidMethod()
        {
            var testCode = @"
public class Foo
{
    private void Bar()
    {
        Meh();
    }

    private void Meh()
    {
    }
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task VoidMethodWithReturn()
        {
            var testCode = @"
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
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task StaticVoidMethod()
        {
            var testCode = @"
public class Foo
{
    private void Bar()
    {
        Meh();
    }

    private static void Meh()
    {
    }
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task StaticVoidMethodWithReturn()
        {
            var testCode = @"
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
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IfTry()
        {
            var testCode = @"
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
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task StringBuilderAppendLine()
        {
            var testCode = @"
using System.Text;
public class Foo
{
    public void Bar()
    {
        var sb = new StringBuilder();
        sb.AppendLine(""test"");
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task StringBuilderAppend()
        {
            var testCode = @"
using System.Text;
public class Foo
{
    public void Bar()
    {
        var sb = new StringBuilder();
        sb.Append(""test"");
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task StringBuilderAppendChained()
        {
            var testCode = @"
using System.Text;
public class Foo
{
    public void Bar()
    {
        var sb = new StringBuilder();
        sb.Append(""1"").Append(""2"");
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenReturningThis()
        {
            var testCode = @"
public class Foo
{
    public Foo Bar()
    {
        return this;
    }

    public void Meh()
    {
        Bar();
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenExtensionMethodReturningThis()
        {
            var barCode = @"
internal static class Bar
{
    internal static T Id<T>(this T value)
    {
        return value;
    }
}";
            var testCode = @"
public class Foo
{
    private Foo()
    {
        var meh =1;
        meh.Id();
    }
}";
            await this.VerifyHappyPathAsync(barCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenThrowing()
        {
            var testCode = @"
using System;
public class Foo
{
    public Foo Bar()
    {
        throw new Exception();
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenInvocationInExpressionBody()
        {
            var testCode = @"
public class Foo
{
    public Foo Bar()
    {
        return this;
    }

    public void Meh() => Bar();
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenNewInExpressionBody()
        {
            var testCode = @"
public class Foo
{
    public void Meh() => new Foo();
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}