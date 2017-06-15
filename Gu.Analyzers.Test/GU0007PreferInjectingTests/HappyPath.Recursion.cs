namespace Gu.Analyzers.Test.GU0007PreferInjectingTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<GU0007PreferInjecting>
    {
        public class Recursion : NestedHappyPathVerifier<HappyPath>
        {
            [Test]
            public async Task IgnoresRecursiveProperty()
            {
                var testCode = @"
    using System;

    public class Foo
    {
        private readonly IDisposable meh1;

        public Foo()
        {
            this.meh1 = this.RecursiveProperty;
        }

        public IDisposable RecursiveProperty => RecursiveProperty;
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [TestCase("this.foo = foo.Inner.Inner;")]
            [TestCase("this.foo = foo?.Inner.Inner;")]
            [TestCase("this.foo = foo?.Inner.Inner?.Inner;")]
            [TestCase("this.foo = foo.Inner?.Inner?.Inner;")]
            [TestCase("this.foo = foo.Inner?.Inner.Inner?.Inner;")]
            public async Task IgnoresRecursivePropertyElvis(string assignment)
            {
                var fooCode = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        private readonly Foo foo;

        public Foo Inner => this.foo;
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Bar
    {
        private readonly Foo foo;

        public Bar(Foo foo)
        {
            this.foo = foo.Inner.Inner;
        }
    }
}";
                testCode = testCode.AssertReplace("this.foo = foo.Inner.Inner;", assignment);
                await this.VerifyHappyPathAsync(fooCode, testCode)
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
        }
    }
}