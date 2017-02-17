namespace Gu.Analyzers.Test.GU0031DisposeMemberTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class HappyPath
    {
        internal class Injected : NestedHappyPathVerifier<HappyPath>
        {
            [Test]
            public async Task IgnorePassedInViaCtorThis()
            {
                var testCode = @"
    using System;

    public sealed class Foo
    {
        private readonly IDisposable bar;
        
        public Foo(IDisposable bar)
        {
            this.bar = bar;
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoreDictionaryPassedInViaCtor()
            {
                var testCode = @"
namespace RoslynSandBox
{
    using System.Collections.Concurrent;
    using System.IO;

    public class Foo
    {
        private readonly Stream current;

        public Foo(ConcurrentDictionary<int, Stream> streams)
        {
            this.current = streams[1];
        }
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnorePassedInViaCtorUnderscore()
            {
                var testCode = @"
    using System;

    public sealed class Foo
    {
        private readonly IDisposable _bar;
        
        public Foo(IDisposable bar)
        {
            _bar = bar;
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnorePassedInViaCtorUnderscoreWhenClassIsDisposable()
            {
                var testCode = @"
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable _bar;
        
        public Foo(IDisposable bar)
        {
            _bar = bar;
        }

        public void Dispose()
        {
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }
        }
    }
}