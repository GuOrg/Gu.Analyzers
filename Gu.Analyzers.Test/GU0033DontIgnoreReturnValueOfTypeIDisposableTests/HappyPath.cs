namespace Gu.Analyzers.Test.GU0033DontIgnoreReturnValueOfTypeIDisposableTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0033DontIgnoreReturnValueOfTypeIDisposable>
    {
        private static readonly string DisposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";

        [Test]
        public async Task ChainedCtor()
        {
            var testCode = @"
using System;

public sealed class Foo : IDisposable
{
    public Foo()
        : this(new Disposable())
    {
    }

    private Foo(IDisposable disposable)
    {
        this.Disposable = disposable;
    }

    public IDisposable Disposable { get; }

    public void Dispose()
    {
        this.Disposable.Dispose();
    }
}";
            await this.VerifyHappyPathAsync(DisposableCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ChainedCtors()
        {
            var testCode = @"
using System;

public sealed class Foo : IDisposable
{
    private readonly int meh;

    public Foo()
        : this(new Disposable())
    {
    }

    private Foo(IDisposable disposable)
        : this(disposable, 1)
    {
    }

    private Foo(IDisposable disposable, int meh)
    {
        this.meh = meh;
        this.Disposable = disposable;
    }

    public IDisposable Disposable { get; }

    public void Dispose()
    {
        this.Disposable.Dispose();
    }
}";
            await this.VerifyHappyPathAsync(DisposableCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ChainedCtorCallsBaseCtorDisposedInThis()
        {
            var baseCode = @"
using System;

public class FooBase : IDisposable
{
    private readonly IDisposable disposable;
    private bool disposed;

    protected FooBase(IDisposable disposable)
    {
        this.disposable = disposable;
    }

    public IDisposable Disposable => this.disposable;

    public void Dispose()
    {
        this.Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
    }
}";

            var testCode = @"
using System;

public sealed class Foo : FooBase
{
    private bool disposed;

    public Foo()
        : this(new Disposable())
    {
    }

    private Foo(IDisposable disposable)
        : base(disposable)
    {
    }

    protected override void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        if (disposing)
        {
            this.Disposable.Dispose();
        }

        base.Dispose(disposing);
    }
}";
            await this.VerifyHappyPathAsync(DisposableCode, baseCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ChainedBaseCtorDisposedInThis()
        {
            var baseCode = @"
using System;

public class FooBase : IDisposable
{
    private readonly object disposable;
    private bool disposed;

    protected FooBase(IDisposable disposable)
    {
        this.disposable = disposable;
    }

    public object Bar
    {
        get
        {
            this.ThrowIfDisposed();
            return this.disposable;
        }
    }

    public void Dispose()
    {
        this.Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
    }

    protected void ThrowIfDisposed()
    {
        if (this.disposed)
        {
            throw new ObjectDisposedException(this.GetType().FullName);
        }
    }
}";

            var testCode = @"
using System;

public sealed class Foo : FooBase
{
    private bool disposed;

    public Foo()
        : base(new Disposable())
    {
    }

    protected override void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        if (disposing)
        {
            (this.Bar as IDisposable)?.Dispose();
        }

        base.Dispose(disposing);
    }
}";
            await this.VerifyHappyPathAsync(DisposableCode, baseCode, testCode)
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
        public async Task MethodReturningObject()
        {
            var testCode = @"
    public class Foo
    {
        public void Bar()
        {
            Meh();
        }

        private static object Meh() => new object();
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task MethodWithArgReturningObject()
        {
            var testCode = @"
    public class Foo
    {
        public void Bar()
        {
            Meh(""Meh"");
        }

        private static object Meh(string arg) => new object();
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task MethodWithObjArgReturningObject()
        {
            var testCode = @"
    public class Foo
    {
        public void Bar()
        {
            Id(new Foo());
        }

        private static object Id(object arg) => arg;
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task Returning()
        {
            var testCode = @"
    using System.IO;

    public class Foo
    {
        public Stream Bar()
        {
            return File.OpenRead(string.Empty);
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ReturningAssigning()
        {
            var fooCode = @"
using System;

public class Foo : IDisposable
{
    private readonly IDisposable disposable;

    public Foo(IDisposable disposable)
        :this()
    {
        this.disposable = disposable;
    }

    public Foo()
    {
    }

    public void Dispose()
    {
        this.disposable.Dispose();
    }
}";
            var testCode = @"
public class Meh
{
    public Foo Bar()
    {
        return new Foo(new Disposable());
    }
}";
            await this.VerifyHappyPathAsync(DisposableCode, fooCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ReturningAssigningPrivateChained()
        {
            var fooCode = @"
using System;

public class Foo : IDisposable
{
    private readonly IDisposable disposable;

    public Foo(IDisposable disposable)
        :this()
    {
        this.disposable = disposable;
    }

    private Foo()
    {
    }

    public void Dispose()
    {
        this.disposable.Dispose();
    }
}";
            var testCode = @"
public class Meh
{
    public Foo Bar()
    {
        return new Foo(new Disposable());
    }
}";
            await this.VerifyHappyPathAsync(DisposableCode, fooCode, testCode)
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
        public async Task AllowPassingIntoStreamReader()
        {
            var testCode = @"
    using System.IO;

    public class Foo
    {
        public StreamReader Bar()
        {
            return new StreamReader(File.OpenRead(string.Empty));
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
        public async Task MatehodWithFuncTaskAsParameter()
        {
            var testCode = @"
using System;
using System.Threading.Tasks;
public class Foo
{
    public void Meh()
    {
        this.Bar(() => Task.Delay(0));
    }
    public void Bar(Func<Task> func)
    {
    }
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task MethodWithFuncStreamAsParameter()
        {
            var testCode = @"
using System;
using System.IO;

public class Foo
{
    public void Meh()
    {
        this.Bar(() => File.OpenRead(string.Empty));
    }

    public void Bar(Func<Stream> func)
    {
    }
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenCreatingStreamReader()
        {
            var testCode = @"
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            using(var reader = new StreamReader(File.OpenRead(string.Empty)))
			{
			}
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task Generic()
        {
            var factoryCode = @"
    public class Factory
    {
        public static T Create<T>() where T : new() => new T();
    }";

            var testCode = @"
    public class Foo
    {
        public void Bar()
        {
            Factory.Create<int>();
        }
    }";
            await this.VerifyHappyPathAsync(factoryCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task Operator()
        {
            var mehCode = @"
    public class Meh
    {
        public static Meh operator +(Meh left, Meh right) => new Meh();
    }";

            var testCode = @"
    public class Foo
    {
        public object Bar()
        {
            var meh1 = new Meh();
            var meh2 = new Meh();
            return meh1 + meh2;
        }
    }";
            await this.VerifyHappyPathAsync(mehCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task OperatorNestedCall()
        {
            var mehCode = @"
    public class Meh
    {
        public static Meh operator +(Meh left, Meh right) => new Meh();
    }";

            var testCode = @"
    public class Foo
    {
        public object Bar()
        {
            var meh1 = new Meh();
            var meh2 = new Meh();
            return Add(new Meh(), new Meh());
        }

        public object Add(Meh meh1, Meh meh2)
        {
            return meh1 + meh2;
        }
    }";
            await this.VerifyHappyPathAsync(mehCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task OperatorEquals()
        {
            var mehCode = @"
    public class Meh
    {
    }";

            var testCode = @"
    public class Foo
    {
        public bool Bar()
        {
            var meh1 = new Meh();
            var meh2 = new Meh();
            return meh1 == meh2;
        }
    }";
            await this.VerifyHappyPathAsync(mehCode, testCode)
                      .ConfigureAwait(false);
        }
    }
}