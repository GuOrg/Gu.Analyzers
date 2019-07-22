namespace Gu.Analyzers.Test.GU0007PreferInjectingTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static partial class Valid
    {
        private static readonly GU0007PreferInjecting Analyzer = new GU0007PreferInjecting();

        private static readonly string Bar = @"
namespace N
{
    public class Bar
    {
        public void Baz()
        {
        }
    }
}";

        private static readonly string WithMutableProperty = @"
namespace N
{
    public class Bar
    {
        public int Baz { get; set; }
    }
}";

        private static readonly string ServiceLocator = @"
namespace N
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
            var code = @"
namespace N
{
    public class C
    {
        private readonly Bar bar;

        public C(Bar bar)
        {
            this.bar = bar;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code, Bar);
        }

        [Test]
        public static void WhenNotInjectingChained()
        {
            var c1 = @"
namespace N
{
    public class C1
    {
        private readonly C2 bar;

        public C1(C2 bar)
        {
            this.bar = bar;
        }
    }
}";
            var bar = @"
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
}";

            var meh = @"
namespace N
{
    public class Meh : C1
    {
        public Meh()
           : base(new C2(1))
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, c1, bar, meh);
        }
    }
}
