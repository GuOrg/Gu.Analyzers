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
namespace N
{
    public class C
    {
        private Bar bar = new Bar();

        private C()
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
namespace N
{
    public static class C
    {
        private static Bar bar;

        static C()
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
namespace N
{
    public class C
    {
        public C()
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
namespace N
{
    public abstract class Abstract
    {
    }
}";

                var barCode = @"
namespace N
{
    public class C2
    {
        private readonly int value;

        public C2(int value)
        {
            this.value = value;
        }
    }
}".AssertReplace("int", type);

                var code = @"
namespace N
{
    public class Foo
    {
        private readonly C2 bar;

        public Foo()
        {
            bar = new C2(default(int));
        }
    }
}".AssertReplace("default(int)", $"default({type})");
                RoslynAssert.Valid(Analyzer, abstractCode, barCode, code);
            }

            [Test]
            public static void WhenParams()
            {
                var abstractCode = @"
namespace N
{
    public class Baz
    {
    }
}";

                var barCode = @"
namespace N
{
    public class C2
    {
        private readonly Baz[] values;

        public C2(params Baz[] values)
        {
            this.values = values;
        }
    }
}";

                var code = @"
namespace N
{
    public class Foo
    {
        private readonly C2 bar;

        public Foo()
        {
            bar = new C2();
        }
    }
}";

                RoslynAssert.Valid(Analyzer, abstractCode, barCode, code);
            }

            [Test]
            public static void NewDictionaryOfBarAndBar()
            {
                var code = @"
namespace N
{
    using System.Collections.Generic;

    public class C
    {
        private readonly Dictionary<Bar, Bar> bar;

        public C()
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
namespace N
{
    using System.Linq;

    public class C
    {
        private ServiceLocator bar;

        public C()
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
namespace N
{
    public class C
    {
        private readonly Bar bar1;
        private readonly Bar bar2;

        public C()
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
namespace N
{
    public class C
    {
        private readonly Bar bar;

        public C()
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
