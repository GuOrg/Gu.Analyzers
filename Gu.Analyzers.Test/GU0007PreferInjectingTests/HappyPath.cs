namespace Gu.Analyzers.Test.GU0007PreferInjectingTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class HappyPath
    {
        private static readonly GU0007PreferInjecting Analyzer = new GU0007PreferInjecting();

        private static readonly string BarCode = @"
namespace RoslynSandbox
{
    public class Bar
    {
        public void Baz()
        {
        }
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
        public void WhenInjecting()
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
            AnalyzerAssert.Valid(Analyzer, fooCode, BarCode);
        }

        [Test]
        public void WhenNotInjectingChained()
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
            AnalyzerAssert.Valid(Analyzer, fooCode, barCode, mehCode);
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

            AnalyzerAssert.Valid(Analyzer, fooCode, BarCode);
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
            AnalyzerAssert.Valid(Analyzer, LocatorCode, BarCode, fooCode);
        }

        [TestCase("int")]
        [TestCase("Abstract")]
        public void IgnoreWhenNewNotInjectable(string type)
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
            AnalyzerAssert.Valid(Analyzer, abstractCode, barCode, fooCode);
        }

        [Test]
        public void IgnoreWhenParams()
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

            AnalyzerAssert.Valid(Analyzer, abstractCode, barCode, fooCode);
        }

        [Test]
        public void IgnoreNewDictionaryOfBarAndBar()
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

            AnalyzerAssert.Valid(Analyzer, BarCode, fooCode);
        }

        [Test]
        public void IgnoreInLambda()
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

            AnalyzerAssert.Valid(Analyzer, BarCode, LocatorCode, fooCode);
        }
    }
}