namespace Gu.Analyzers.Test.GU0036DontDisposeInjectedTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0036DontDisposeInjected>
    {
        [Test]
        public async Task NotIDisposable()
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
        public async Task NotDisposingField()
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
    }
}