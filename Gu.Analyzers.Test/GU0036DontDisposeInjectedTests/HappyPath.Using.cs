namespace Gu.Analyzers.Test.GU0036DontDisposeInjectedTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class HappyPath
    {
        public class Using : NestedHappyPathVerifier<HappyPath>
        {
            [Test]
            public async Task FileOpenRead()
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
            public async Task FileOpenReadVariable()
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
            public async Task AwaitedStream()
            {
                var testCode = @"
using System.IO;
using System.Threading.Tasks;

public static class Foo
{
    public static async Task Bar()
    {
        using (await ReadAsync(string.Empty).ConfigureAwait(false))
        {
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
            public async Task AwaitedStreamVariable()
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
            public async Task Enumerator()
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
            public async Task CreatedUsingInjectedConcreteFactory()
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
            public async Task CreatedUsingInjectedInterfaceFactory()
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
            public async Task CreatedUsingInjectedAbstractFactoryWIthImplementation()
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
                await this.VerifyHappyPathAsync(abstractFactoryCode, factoryCode, disposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task CreatedUsingInjectedGenericAbstractFactoryWIthImplementation()
            {
                var abstractFactoryCode = @"
using System;

public abstract class FactoryBase<T>
{
    public abstract IDisposable Create();
}";
                var factoryCode = @"
using System;

public class Factory : FactoryBase<int>
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
    public static void Bar<T>(FactoryBase<T> factory)
    {
        using (factory.Create())
        {
        }
    }
}";
                await this.VerifyHappyPathAsync(abstractFactoryCode, factoryCode, disposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task CreatedUsingInjectedAbstractFactoryNoImplementation()
            {
                var abstractFactoryCode = @"
using System;

public abstract class FactoryBase
{
    public abstract IDisposable Create();
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
                await this.VerifyHappyPathAsync(abstractFactoryCode, disposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task CreatedUsingInjectedGenericAbstractFactoryNoImplementation()
            {
                var abstractFactoryCode = @"
using System;

public abstract class FactoryBase<T>
{
    public abstract IDisposable Create();
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
    public Foo(FactoryBase<int> factory)
    {
        using (factory.Create())
        {
        }
    }
}";
                await this.VerifyHappyPathAsync(abstractFactoryCode, disposableCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task CreatedUsingInjectedVirtualFactory()
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
}