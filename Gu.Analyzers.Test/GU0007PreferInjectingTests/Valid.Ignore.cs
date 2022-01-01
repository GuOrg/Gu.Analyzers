namespace Gu.Analyzers.Test.GU0007PreferInjectingTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal partial class Valid
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

            RoslynAssert.Valid(Analyzer, code, Bar);
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

            RoslynAssert.Valid(Analyzer, code, Bar);
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
            RoslynAssert.Valid(Analyzer, ServiceLocator, Bar, code);
        }

        [TestCase("int")]
        [TestCase("Abstract?")]
        public static void WhenNewNotInjectable(string type)
        {
            var @abstract = @"
namespace N
{
    public abstract class Abstract
    {
    }
}";

            var c2 = @"
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
    public class C1
    {
        private readonly C2 bar;

        public C1()
        {
            bar = new C2(default);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, @abstract, c2, code);
        }

        [Test]
        public static void WhenParams()
        {
            var baz = @"
namespace N
{
    public class Baz
    {
    }
}";

            var c2 = @"
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
    public class C1
    {
        private readonly C2 c2;

        public C1()
        {
            c2 = new C2();
        }
    }
}";

            RoslynAssert.Valid(Analyzer, baz, c2, code);
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
        private readonly Dictionary<Bar, Bar> map;

        public C()
        {
            this.map = new Dictionary<Bar, Bar>();
        }
    }
}";

            RoslynAssert.Valid(Analyzer, Bar, code);
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
        private ServiceLocator? locator;

        public C()
        {
            this.locator = new ServiceLocator[0].FirstOrDefault(x => x.Bar != null);
        }
    }
}";

            RoslynAssert.Valid(Analyzer, Bar, ServiceLocator, code);
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

            RoslynAssert.Valid(Analyzer, code, Bar);
        }

        [Test]
        public static void WhenAssigningWithObjectInitializer()
        {
            var c2 = @"
namespace N
{
    public class C2
    {
        public int Baz { get; set; }
    }
}";

            var code = @"
namespace N
{
    public class C
    {
        private readonly C2 bar;

        public C()
        {
            this.bar = new C2 { Baz = 1 };
        }
    }
}";

            RoslynAssert.Valid(Analyzer, c2, code);
        }
    }
}