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
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly Bar bar;

        public Foo(Bar bar)
        {
            this.bar = bar;
        }
    }
}";
            var barCode = @"
namespace RoslynSandbox
{
    public class Bar
    {
        private readonly int value;

        public Bar(int value)
        {
            this.value = value;
        }
    }
}";

            var mehCode = @"
namespace RoslynSandbox
{
    public class Meh : Foo
    {
        public Meh()
           : base(new Bar(1))
        {
        }
    }
}";
            await this.VerifyHappyPathAsync(fooCode, barCode, mehCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenStatic()
        {
            var fooCode = @"
namespace RoslynSandbox
{
    public static class Foo
    {
        private static Bar bar;

        static Foo()
        {
            bar = new Bar();
        }
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
        public async Task IgnoreWhenNewNotInjectable(string type)
        {
            var abstractCode = @"
namespace RoslynSandbox
{
    public abstract class Abstract
    {
    }
}";

            var barCode = @"
namespace RoslynSandbox
{
    public class Bar
    {
        private readonly int value;

        public Bar(int value)
        {
            this.value = value;
        }
    }
}";

            barCode = barCode.AssertReplace("int", type);

            var fooCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly Bar bar;

        public Foo()
        {
            bar = new Bar(default(int));
        }
    }
}";

            fooCode = fooCode.AssertReplace("default(int)", $"default({type})");
            await this.VerifyHappyPathAsync(abstractCode, barCode, fooCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoreWhenParams()
        {
            var abstractCode = @"
namespace RoslynSandbox
{
    public class Baz
    {
    }
}";

            var barCode = @"
namespace RoslynSandbox
{
    public class Bar
    {
        private readonly Baz[] values;

        public Bar(params Baz[] values)
        {
            this.values = values;
        }
    }
}";

            var fooCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly Bar bar;

        public Foo()
        {
            bar = new Bar();
        }
    }
}";

            await this.VerifyHappyPathAsync(abstractCode, barCode, fooCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoreNewDictionaryOfBarAndBar()
        {
            var fooCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    public class Foo
    {
        private readonly Dictionary<Bar, Bar> bar;

        public Foo()
        {
            this.bar = new Dictionary<Bar, Bar>();
        }
    }
}";

            await this.VerifyHappyPathAsync(BarCode, fooCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoreInLambda()
        {
            var fooCode = @"
namespace RoslynSandbox
{
    using System.Linq;

    public class Foo
    {
        private ServiceLocator bar;

        public Foo()
        {
            this.bar = new ServiceLocator[0].FirstOrDefault(x => x.Bar != null);
        }
    }
}";

            await this.VerifyHappyPathAsync(BarCode, LocatorCode, fooCode)
                      .ConfigureAwait(false);
        }
    }
}