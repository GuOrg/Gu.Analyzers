namespace Gu.Analyzers.Test.GU0007PreferInjectingTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static partial class CodeFix
    {
        internal static class ServiceLocatorTests
        {
            private const string Bar = @"
namespace N
{
    public class Bar
    {
        public void Baz()
        {
        }

        public void Baz(int i)
        {
        }
    }
}";

            private const string AbstractC = @"
namespace N
{
    public abstract class AbstractC
    {
        private readonly Bar bar;

        protected AbstractC(Bar bar)
        {
            this.bar = bar;
        }
    }
}";

            private const string ServiceLocator = @"
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
            public static void AssigningThisFieldInCtor()
            {
                var before = @"
namespace N
{
    public class C
    {
        private readonly Bar value;

        public C(ServiceLocator locator)
        {
            this.value = locator.↓Bar;
        }
    }
}";

                var after = @"
namespace N
{
    public class C
    {
        private readonly Bar value;

        public C(ServiceLocator locator, Bar value)
        {
            this.value = value;
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Bar, ServiceLocator, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void AssigningUnderscoreFieldInCtor()
            {
                var before = @"
namespace N
{
    public class C
    {
        private readonly Bar _value;

        public C(ServiceLocator locator)
        {
            _value = locator.↓Bar;
        }
    }
}";

                var after = @"
namespace N
{
    public class C
    {
        private readonly Bar _value;

        public C(ServiceLocator locator, Bar value)
        {
            _value = value;
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Bar, ServiceLocator, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenNotInjectingFieldInitializationWithNameCollision()
            {
                var enumCode = @"
namespace N
{
    public enum Meh
    {
        Bar
    }
}";
                var before = @"
namespace N
{
    public class C
    {
        private readonly Bar bar;

        public C(ServiceLocator locator, Meh meh)
        {
            this.bar = locator.↓Bar;
            if (meh == Meh.Bar)
            {
            }
        }
    }
}";

                var after = @"
namespace N
{
    public class C
    {
        private readonly Bar bar;

        public C(ServiceLocator locator, Meh meh, Bar bar)
        {
            this.bar = bar;
            if (meh == Meh.Bar)
            {
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Bar, ServiceLocator, enumCode, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void FieldInitializationAndBaseCall()
            {
                var before = @"
namespace N
{
    public class C : AbstractC
    {
        private readonly Bar bar;

        public C(ServiceLocator locator)
            : base(locator.↓Bar)
        {
            this.bar = locator.↓Bar;
        }
    }
}";

                var after = @"
namespace N
{
    public class C : AbstractC
    {
        private readonly Bar bar;

        public C(ServiceLocator locator, Bar bar)
            : base(bar)
        {
            this.bar = bar;
        }
    }
}";
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Bar, ServiceLocator, AbstractC, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void FieldInitializationAndBaseCallUnderscoreNames()
            {
                var before = @"
namespace N
{
    public class C : AbstractC
    {
        private readonly Bar _bar;

        public C(ServiceLocator locator)
            : base(locator.↓Bar)
        {
            _bar = locator.↓Bar;
        }
    }
}";

                var after = @"
namespace N
{
    public class C : AbstractC
    {
        private readonly Bar _bar;

        public C(ServiceLocator locator, Bar bar)
            : base(bar)
        {
            _bar = bar;
        }
    }
}";
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Bar, ServiceLocator, AbstractC, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenUsingMethodInjectedLocator()
            {
                var before = @"
namespace N
{
    public class C
    {
        public C()
        {
        }

        public void Meh(ServiceLocator locator)
        {
            locator.↓Bar.Baz();
        }
    }
}";

                var after = @"
namespace N
{
    public class C
    {
        private readonly Bar bar;

        public C(Bar bar)
        {
            this.bar = bar;
        }

        public void Meh(ServiceLocator locator)
        {
            this.bar.Baz();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Bar, ServiceLocator, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenUsingLocatorInMethod()
            {
                var before = @"
namespace N
{
    public class C
    {
        private readonly ServiceLocator locator;

        public C(ServiceLocator locator)
        {
            this.locator = locator;
        }

        public void Meh()
        {
            this.locator.↓Bar.Baz();
        }
    }
}";

                var after = @"
namespace N
{
    public class C
    {
        private readonly Bar bar;
        private readonly ServiceLocator locator;

        public C(ServiceLocator locator, Bar bar)
        {
            this.bar = bar;
            this.locator = locator;
        }

        public void Meh()
        {
            this.bar.Baz();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Bar, ServiceLocator, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenUsingLocatorInLambdaClosure()
            {
                var before = @"
namespace N
{
    using System.Collections.Generic;
    using System.Linq;

    public class C
    {
        private readonly ServiceLocator locator;

        public C(IEnumerable<ServiceLocator> bars, ServiceLocator locator)
        {
            this.locator = bars.First(x => x.Bar == locator.↓Bar);
        }
    }
}";

                var after = @"
namespace N
{
    using System.Collections.Generic;
    using System.Linq;

    public class C
    {
        private readonly ServiceLocator locator;

        public C(IEnumerable<ServiceLocator> bars, ServiceLocator locator, Bar bar)
        {
            this.locator = bars.First(x => x.Bar == bar);
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Bar, ServiceLocator, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenUsingLocatorInTwoMethods()
            {
                var before = @"
namespace N
{
    public class C
    {
        private readonly ServiceLocator locator;

        public C(ServiceLocator locator)
        {
            this.locator = locator;
        }

        public void Meh1()
        {
            this.locator.↓Bar.Baz();
        }

        public void Meh2()
        {
            this.locator.↓Bar.Baz(2);
        }
    }
}";

                var after = @"
namespace N
{
    public class C
    {
        private readonly Bar bar;
        private readonly ServiceLocator locator;

        public C(ServiceLocator locator, Bar bar)
        {
            this.bar = bar;
            this.locator = locator;
        }

        public void Meh1()
        {
            this.bar.Baz();
        }

        public void Meh2()
        {
            this.bar.Baz(2);
        }
    }
}";
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Bar, ServiceLocator, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenUsingLocatorInMethodUnderscoreNames()
            {
                var before = @"
namespace N
{
    public class C
    {
        private readonly ServiceLocator _locator;

        public C(ServiceLocator locator)
        {
            _locator = locator;
        }

        public void Meh()
        {
            _locator.↓Bar.Baz();
        }
    }
}";

                var after = @"
namespace N
{
    public class C
    {
        private readonly Bar _bar;
        private readonly ServiceLocator _locator;

        public C(ServiceLocator locator, Bar bar)
        {
            _bar = bar;
            _locator = locator;
        }

        public void Meh()
        {
            _bar.Baz();
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Bar, ServiceLocator, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenUsingLocatorInMethodAndBaseCall()
            {
                var before = @"
namespace N
{
    public class C : AbstractC
    {
        private readonly ServiceLocator locator;

        public C(ServiceLocator locator)
            : base(locator.↓Bar)
        {
            this.locator = locator;
        }

        public void Meh()
        {
            this.locator.↓Bar.Baz();
        }
    }
}";

                var after = @"
namespace N
{
    public class C : AbstractC
    {
        private readonly Bar bar;
        private readonly ServiceLocator locator;

        public C(ServiceLocator locator, Bar bar)
            : base(bar)
        {
            this.bar = bar;
            this.locator = locator;
        }

        public void Meh()
        {
            this.bar.Baz();
        }
    }
}";
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Bar, ServiceLocator, AbstractC, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenUsingLocatorInStaticMethod()
            {
                var before = @"
namespace N
{
    public class C : AbstractC
    {
        private readonly Bar bar;

        public C(ServiceLocator locator)
            : base(locator.↓Bar)
        {
            this.bar = locator.↓Bar;
        }

        public static void Meh(ServiceLocator locator)
        {
            locator.Bar.Baz();
        }
    }
}";

                var after = @"
namespace N
{
    public class C : AbstractC
    {
        private readonly Bar bar;

        public C(ServiceLocator locator, Bar bar)
            : base(bar)
        {
            this.bar = bar;
        }

        public static void Meh(ServiceLocator locator)
        {
            locator.Bar.Baz();
        }
    }
}";
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { Bar, ServiceLocator, AbstractC, before }, after, fixTitle: "Inject safe.");
            }
        }
    }
}
