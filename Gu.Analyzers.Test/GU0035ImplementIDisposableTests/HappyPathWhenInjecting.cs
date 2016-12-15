namespace Gu.Analyzers.Test.GU0035ImplementIDisposableTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class HappyPathWhenInjecting : HappyPathVerifier<GU0035ImplementIDisposable>
    {
        [Test]
        public async Task FactoryMethodCallingPrivateCtor()
        {
            var testCode = @"
    public class Foo
    {
        private readonly bool value;

        private Foo(bool value)
        {
            this.value = value;
        }

        public static Foo Create() => new Foo(true);
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task FactoryMethodCallingPrivateCtorWithCachedDisposable()
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

public sealed class Foo
{
    private static readonly IDisposable Cached = new Disposable();
    private readonly IDisposable value;

    private Foo(IDisposable value)
    {
        this.value = value;
    }

    public static Foo Create() => new Foo(Cached);
}";
            await this.VerifyHappyPathAsync(disposableCode, testCode)
                      .ConfigureAwait(false);
        }
    }
}