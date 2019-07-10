namespace Gu.Analyzers.Test.GU0007PreferInjectingTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class ValidCode
    {
        internal static class Ignore
        {
            [Test]
            public static void WhenPrivateCtor()
            {
                var code = @"
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

                RoslynAssert.Valid(Analyzer, code, BarCode);
            }

            [Test]
            public static void WhenStatic()
            {
                var code = @"
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

                RoslynAssert.Valid(Analyzer, code, BarCode);
            }

            [Test]
            public static void WhenMethodInjectedLocatorInStaticMethod()
            {
                var code = @"
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
                RoslynAssert.Valid(Analyzer, LocatorCode, BarCode, code);
            }

            [TestCase("int")]
            [TestCase("Abstract")]
            public static void WhenNewNotInjectable(string type)
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

                var code = @"
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
                RoslynAssert.Valid(Analyzer, abstractCode, barCode, code);
            }

            [Test]
            public static void WhenParams()
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

                var code = @"
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

                RoslynAssert.Valid(Analyzer, abstractCode, barCode, code);
            }

            [Test]
            public static void NewDictionaryOfBarAndBar()
            {
                var code = @"
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

                RoslynAssert.Valid(Analyzer, BarCode, code);
            }

            [Test]
            public static void InLambda()
            {
                var code = @"
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

                RoslynAssert.Valid(Analyzer, BarCode, LocatorCode, code);
            }

            [Test]
            public static void WhenAssigningTwoFieldWithObjectCreations()
            {
                var code = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly Bar bar1;
        private readonly Bar bar2;

        public Foo()
        {
            this.bar1 = new Bar();
            this.bar2 = new Bar();
        }
    }
}";

                RoslynAssert.Valid(Analyzer, code, BarCode);
            }

            [Test]
            public static void WhenAssigningWithObjectInitializer()
            {
                var code = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly Bar bar;

        public Foo()
        {
            this.bar = new Bar { Baz = 1 };
        }
    }
}";

                RoslynAssert.Valid(Analyzer, code, WithMutableProperty);
            }
        }
    }
}
