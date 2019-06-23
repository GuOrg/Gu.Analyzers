namespace Gu.Analyzers.Test.GU0007PreferInjectingTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class ValidCode
    {
        internal class Ignore
        {
            [Test]
            public void WhenPrivateCtor()
            {
                var fooCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private Bar bar = new Bar();

        private Foo()
        {
        }
    }
}";

                RoslynAssert.Valid(Analyzer, fooCode, BarCode);
            }

            [Test]
            public void WhenStatic()
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

                RoslynAssert.Valid(Analyzer, fooCode, BarCode);
            }

            [Test]
            public void WhenMethodInjectedLocatorInStaticMethod()
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
                RoslynAssert.Valid(Analyzer, LocatorCode, BarCode, fooCode);
            }

            [TestCase("int")]
            [TestCase("Abstract")]
            public void WhenNewNotInjectable(string type)
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
}".AssertReplace("int", type);

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
}".AssertReplace("default(int)", $"default({type})");
                RoslynAssert.Valid(Analyzer, abstractCode, barCode, fooCode);
            }

            [Test]
            public void WhenParams()
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

                RoslynAssert.Valid(Analyzer, abstractCode, barCode, fooCode);
            }

            [Test]
            public void NewDictionaryOfBarAndBar()
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

                RoslynAssert.Valid(Analyzer, BarCode, fooCode);
            }

            [Test]
            public void InLambda()
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

                RoslynAssert.Valid(Analyzer, BarCode, LocatorCode, fooCode);
            }
        }
    }
}
