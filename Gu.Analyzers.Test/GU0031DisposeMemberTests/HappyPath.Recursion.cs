namespace Gu.Analyzers.Test.GU0031DisposeMemberTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class HappyPath
    {
        public class Recursion : NestedHappyPathVerifier<HappyPath>
        {
            [Test]
            public async Task IgnoresWhenDisposingRecursiveProperty()
            {
                var testCode = @"
    using System;

    public class Foo : IDisposable
    {
        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
            this.RecursiveProperty.Dispose();
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoresWhenNotDisposingRecursiveProperty()
            {
                var testCode = @"
    using System;

    public class Foo : IDisposable
    {
        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoresWhenDisposingFieldAssignedWithRecursiveProperty()
            {
                var testCode = @"
    using System;

    public class Foo : IDisposable
    {
        private IDisposable disposable;

        public Foo()
        {
            this.disposable = this.RecursiveProperty;
        }

        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoresWhenNotDisposingFieldAssignedWithRecursiveProperty()
            {
                var testCode = @"
    using System;

    public class Foo : IDisposable
    {
        private IDisposable disposable;

        public Foo()
        {
            this.disposable = this.RecursiveProperty;
        }

        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoresWhenDisposingRecursiveMethod()
            {
                var testCode = @"
using System;

public class Foo
{
    public IDisposable RecursiveMethod() => RecursiveMethod();

    public void Dispose()
    {
        this.RecursiveMethod().Dispose();
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoresWhenDisposingRecursiveMethodChain()
            {
                var testCode = @"
using System;

public class Foo
{
    public IDisposable Recursive1() => Recursive2();

    public IDisposable Recursive2() => Recursive1();

    public void Dispose()
    {
        this.RecursiveMethod().Dispose();
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoresRecursiveOutParameter()
            {
                var testCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private readonly Stream stream;

    public Foo()
    {
        if(TryGetStream(out this.stream))
        {
        }
    }

    public bool TryGetStream(out Stream outValue)
    {
        return TryGetStream(out Stream outValue);
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoresRecursiveOutParameterChain()
            {
                var testCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private readonly Stream stream;

    public Foo()
    {
        if(TryGetStream(out this.stream))
        {
        }
    }

    public bool TryGetStream1(out Stream outValue)
    {
        return TryGetStream2(out Stream outValue);
    }

    public bool TryGetStream2(out Stream outValue)
    {
        return TryGetStream1(out Stream outValue);
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }
        }
    }
}