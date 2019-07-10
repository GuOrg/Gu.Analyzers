namespace Gu.Analyzers.Test.GU0007PreferInjectingTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static partial class ValidCode
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

        private static readonly string WithMutableProperty = @"
namespace RoslynSandbox
{
    public class Bar
    {
        public int Baz { get; set; }
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
        public static void WhenInjecting()
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
            RoslynAssert.Valid(Analyzer, fooCode, BarCode);
        }

        [Test]
        public static void WhenNotInjectingChained()
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
            RoslynAssert.Valid(Analyzer, fooCode, barCode, mehCode);
        }
    }
}
