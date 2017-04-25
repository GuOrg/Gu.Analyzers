namespace Gu.Analyzers.Test.GU0007PreferInjectingTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<GU0007PreferInjecting>
    {
        private static readonly string BarCode = @"
    public class Bar
    {
        public void Baz()
        {
        }
    }";

        private static readonly string LocatorCode = @"
namespace RoslynSandbox
{
    public class ServiceLocator
    {
        public ServiceLocator(Bar bar)
        {
            this.Bar = bar;
            this.BarObject = bar;
        }

        public Bar Bar { get; }

        public object BarObject { get; }
    }
}";
        [Test]
        public async Task WhenInjecting()
        {
            var fooCode = @"
    public class Foo
    {
        private readonly Bar bar;

        public Foo(Bar bar)
        {
            this.bar = bar;
        }
    }";

            await this.VerifyHappyPathAsync(fooCode, BarCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenNotInjectingChained()
        {
            var fooCode = @"
    public class Foo
    {
        private readonly Bar bar;

        public Foo(Bar bar)
        {
            this.bar = bar;
        }
    }";
            var barCode = @"
    public class Bar
    {
        private readonly int value;

        public Bar(int value)
        {
            this.value = value;
        }
    }";

            var mehCode = @"
    public class Meh : Foo
    {
        public Meh()
           : base(new Bar(1))
        {
        }
    }";
            await this.VerifyHappyPathAsync(new[] { fooCode, barCode, mehCode })
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenStatic()
        {
            var fooCode = @"
    public static class Foo
    {
        private static Bar bar;

        static Foo()
        {
            bar = new Bar();
        }
    }";

            await this.VerifyHappyPathAsync(fooCode, BarCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenMethodInjectedLocatorInStaticMethod()
        {
            var fooCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo()
        {
        }

        public static void Meh(ServiceLocator locator)
        {
            locator.Bar.Baz();
        }
    }
}";
            await this.VerifyHappyPathAsync(LocatorCode, BarCode, fooCode)
                      .ConfigureAwait(false);
        }

        [TestCase("int")]
        [TestCase("Abstract")]
        public async Task WhenNewNotInjectable(string type)
        {
            var abstractCode = @"
    public abstract class Abstract
    {
    }";

            var barCode = @"
    public class Bar
    {
        private readonly int value;

        public Bar(int value)
        {
            this.value = value;
        }
    }";

            barCode = barCode.AssertReplace("int", type);

            var fooCode = @"
    public class Foo
    {
        private readonly Bar bar;

        public Foo()
        {
            bar = new Bar(default(int));
        }
    }";

            fooCode = fooCode.AssertReplace("default(int)", $"default({type})");
            await this.VerifyHappyPathAsync(abstractCode, barCode, fooCode)
                      .ConfigureAwait(false);
        }
    }
}