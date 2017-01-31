namespace Gu.Analyzers.Test.GU0036DontDisposeInjectedTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0036DontDisposeInjected>
    {
        [Test]
        public async Task InjectedInClassThatIsNotIDisposable()
        {
            var testCode = @"
    using System;

    public sealed class Foo
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedInClassThatIsIDisposable()
        {
            var testCode = @"
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedInClassThatIsIDisposableManyCtors()
        {
            var testCode = @"
using System;

public sealed class Foo : IDisposable
{
    private readonly IDisposable disposable;

    public Foo(IDisposable disposable)
        : this(disposable, ""meh"")
    {
    }

    public Foo(IDisposable disposable, IDisposable gah, int meh)
        : this(disposable, meh)
    {
    }

    private Foo(IDisposable disposable, int meh)
        : this(disposable, meh.ToString())
    {
    }

    private Foo(IDisposable disposable, string meh)
    {
        this.disposable = disposable;
    }

    public void Dispose()
    {
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedObjectInClassThatIsIDisposableWhenTouchingInjectedInDisposeMethod()
        {
            var testCode = @"
    using System;

    public sealed class Foo : IDisposable
    {
        private object meh;

        public Foo(object meh)
        {
            this.meh = meh;
        }

        public void Dispose()
        {
            meh = null;
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingFieldInVirtualDispose()
        {
            var testCode = @"
    using System;
    using System.IO;

    public class Foo : IDisposable
    {
        private readonly IDisposable disposable;
        private bool disposed;

        public Foo(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        protected void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InjectingIntoPrivateCtor()
        {
            var disposableCode = @"
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }";

            var testCode = @"
using System;

public sealed class Foo : IDisposable
{
    private readonly IDisposable disposable;

    public Foo()
        : this(new Disposable())
    {
    }

    private Foo(IDisposable disposable)
    {
        this.disposable = disposable;
    }

    public void Dispose()
    {
        this.disposable.Dispose();
    }
}";
            await this.VerifyHappyPathAsync(disposableCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task UsingStream1()
        {
            var testCode = @"
    using System.IO;

    public class Foo
    {
        public Foo()
        {
            using (File.OpenRead(string.Empty))
            {
            }
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task UsingStream2()
        {
            var testCode = @"
    using System.IO;

    public class Foo
    {
        public Foo()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
            }
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task UsingAwaitedStream()
        {
            var testCode = @"
using System.IO;
using System.Threading.Tasks;

public static class Foo
{
    public static async Task<long> Bar()
    {
        using (var stream = await ReadAsync(string.Empty).ConfigureAwait(false))
        {
            return stream.Length;
        }
    }

    private static async Task<MemoryStream> ReadAsync(string fileName)
    {
        var stream = new MemoryStream();
        using (var fileStream = File.OpenRead(fileName))
        {
            await fileStream.CopyToAsync(stream)
                            .ConfigureAwait(false);
        }

        stream.Position = 0;
        return stream;
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task UsingEnumerator()
        {
            var testCode = @"
    using System.Collections.Generic;

    public static class Ext
    {
        public static TSource Foo<TSource>(this IEnumerable<TSource> source)
        {
            using (var e = source.GetEnumerator())
            {
                return default(TSource);
            }
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task UsingCreatedUsingInjectedConcreteFactory()
        {
            var factoryCode = @"
using System;

public class Factory
{
    public IDisposable Create()
    {
        return new Disposable();
    }
}";
            var disposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";

            var testCode = @"
public class Foo
{
    public Foo(Factory factory)
    {
        using (factory.Create())
        {
        }
    }
}";
            await this.VerifyHappyPathAsync(new[] { factoryCode, disposableCode, testCode })
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task UsingCreatedUsingInjectedInterfaceFactory()
        {
            var iFactoryCode = @"
using System;

public interface IFactory
{
    IDisposable Create();
}";
            var factoryCode = @"
using System;

public class Factory : IFactory
{
    public IDisposable Create()
    {
        return new Disposable();
    }
}";
            var disposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";

            var testCode = @"
public class Foo
{
    public Foo(IFactory factory)
    {
        using (factory.Create())
        {
        }
    }
}";
            await this.VerifyHappyPathAsync(new[] { iFactoryCode, factoryCode, disposableCode, testCode })
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task UsingCreatedUsingInjectedAbstractFactory()
        {
            var abstractFactoryCode = @"
using System;

public abstract class FactoryBase
{
    public abstract IDisposable Create();
}";
            var factoryCode = @"
using System;

public class Factory : FactoryBase
{
    public override IDisposable Create()
    {
        return new Disposable();
    }
}";
            var disposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";

            var testCode = @"
public class Foo
{
    public Foo(FactoryBase factory)
    {
        using (factory.Create())
        {
        }
    }
}";
            await this.VerifyHappyPathAsync(new[] { abstractFactoryCode, factoryCode, disposableCode, testCode })
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task UsingCreatedUsingInjectedVirtualFactory()
        {
            var abstractFactoryCode = @"
using System;

public class FactoryBase
{
    public virtual IDisposable Create() => new Disposable();
}";
            var factoryCode = @"
using System;

public class Factory : FactoryBase
{
    public override IDisposable Create()
    {
        return new Disposable();
    }
}";
            var disposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";

            var testCode = @"
public class Foo
{
    public Foo(FactoryBase factory)
    {
        using (factory.Create())
        {
        }
    }
}";
            await this.VerifyHappyPathAsync(new[] { abstractFactoryCode, factoryCode, disposableCode, testCode })
                      .ConfigureAwait(false);
        }
    }
}