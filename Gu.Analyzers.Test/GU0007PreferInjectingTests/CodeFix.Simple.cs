namespace Gu.Analyzers.Test.GU0007PreferInjectingTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static partial class CodeFix
    {
        internal static class Simple
        {
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

            [Test]
            public static void WhenAssigningThisFieldWithObjectCreation()
            {
                var before = @"
namespace N
{
    public class Foo
    {
        private readonly Bar bar;

        public Foo()
        {
            this.bar = ↓new Bar();
        }
    }
}";

                var after = @"
namespace N
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
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic.WithMessage("Prefer injecting Bar."), before, Bar);
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { before, Bar }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenAssigningUnderscoreFieldWithObjectCreation()
            {
                var before = @"
namespace N
{
    public class C
    {
        private readonly Bar _bar;

        public C()
        {
            _bar = ↓new Bar();
        }
    }
}";

                var after = @"
namespace N
{
    public class C
    {
        private readonly Bar _bar;

        public C(Bar bar)
        {
            _bar = bar;
        }
    }
}";
                var expectedDiagnostic = ExpectedDiagnostic.WithMessage("Prefer injecting Bar.");
                RoslynAssert.CodeFix(Analyzer, Fix, expectedDiagnostic, new[] { before, Bar }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenAssigningThisPropertyWithObjectCreation()
            {
                var before = @"
namespace N
{
    public class Foo
    {
        public Foo()
        {
            this.Bar = ↓new Bar();
        }

        public Bar Bar { get; }
    }
}";

                var after = @"
namespace N
{
    public class Foo
    {
        public Foo(Bar bar)
        {
            this.Bar = bar;
        }

        public Bar Bar { get; }
    }
}";
                var expectedDiagnostic = ExpectedDiagnostic.WithMessage("Prefer injecting Bar.");
                RoslynAssert.CodeFix(Analyzer, Fix, expectedDiagnostic, new[] { before, Bar }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenAssigningPropertyWithObjectCreation()
            {
                var code = @"
namespace N
{
    public class Foo
    {
        public Foo()
        {
            Bar = ↓new Bar();
        }

        public Bar Bar { get; }
    }
}";

                var after = @"
namespace N
{
    public class Foo
    {
        public Foo(Bar bar)
        {
            Bar = bar;
        }

        public Bar Bar { get; }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic.WithMessage("Prefer injecting Bar."), code, Bar);
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { code, Bar }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenNotInjectingChained()
            {
                var fooCode = @"
namespace N
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

                var before = @"
namespace N
{
    public class Meh : Foo
    {
        public Meh()
           : base(↓new Bar())
        {
        }
    }
}";

                var after = @"
namespace N
{
    public class Meh : Foo
    {
        public Meh(Bar bar)
           : base(bar)
        {
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { fooCode, Bar, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenNotInjectingChainedNameCollision()
            {
                var fooCode = @"
namespace N
{
    public class Foo
    {
        private readonly Bar bar;

        public Foo(int value, Bar bar)
        {
            this.bar = bar;
        }
    }
}";

                var before = @"
namespace N
{
    public class Meh : Foo
    {
        public Meh(int bar)
           : base(bar, ↓new Bar())
        {
        }
    }
}";

                var after = @"
namespace N
{
    public class Meh : Foo
    {
        public Meh(int bar, Bar bar_)
           : base(bar, bar_)
        {
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { fooCode, Bar, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenNotInjectingOptional()
            {
                var before = @"
namespace N
{
    public class Foo
    {
        private readonly Bar bar;

        public Foo(int value = 1)
        {
            this.bar = ↓new Bar();
        }
    }
}";

                var after = @"
namespace N
{
    public class Foo
    {
        private readonly Bar bar;

        public Foo(Bar bar, int value = 1)
        {
            this.bar = bar;
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Bar, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenNotInjectingParams()
            {
                var before = @"
namespace N
{
    public class Foo
    {
        private readonly Bar bar;

        public Foo(params int[] values)
        {
            this.bar = ↓new Bar();
        }
    }
}";

                var after = @"
namespace N
{
    public class Foo
    {
        private readonly Bar bar;

        public Foo(Bar bar, params int[] values)
        {
            this.bar = bar;
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Bar, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenNotInjectingChainedGeneric()
            {
                var fooCode = @"
namespace N
{
    public class Foo
    {
        private readonly Bar<int> bar;

        public Foo(Bar<int> bar)
        {
            this.bar = bar;
        }
    }
}";
                var barCode = @"
namespace N
{
    public class Bar<T>
    {
    }
}";

                var before = @"
namespace N
{
    public class Meh : Foo
    {
        public Meh()
           : base(↓new Bar<int>())
        {
        }
    }
}";

                var after = @"
namespace N
{
    public class Meh : Foo
    {
        public Meh(Bar<int> bar)
           : base(bar)
        {
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { fooCode, barCode, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenNotInjectingChainedGenericParameterName()
            {
                var before = @"
namespace N
{
    public sealed class Foo
    {
        private readonly IsModuleReady<FooModule> isFooReady;

        public Foo()
        {
            this.isFooReady = ↓new IsModuleReady<FooModule>();
        }
    }
}";

                var moduleCode = @"
namespace N
{
    public class IsModuleReady<TModule>
        where TModule : Module
    {
    }

    public abstract class Module { }

    public class FooModule : Module { }
}";

                var after = @"
namespace N
{
    public sealed class Foo
    {
        private readonly IsModuleReady<FooModule> isFooReady;

        public Foo(IsModuleReady<FooModule> isFooReady)
        {
            this.isFooReady = isFooReady;
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { before, moduleCode }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenNotInjectingChainedNewWithInjectedArgument()
            {
                var fooCode = @"
namespace N
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
namespace N
{
    public class Bar
    {
        private readonly Baz baz;

        public Bar(Baz baz)
        {
            this.baz = baz;
        }
    }
}";

                var bazCode = @"
namespace N
{
    public class Baz
    {
    }
}";
                var before = @"
namespace N
{
    public class Meh : Foo
    {
        public Meh(Baz baz)
           : base(↓new Bar(baz))
        {
        }
    }
}";

                var after = @"
namespace N
{
    public class Meh : Foo
    {
        public Meh(Baz baz, Bar bar)
           : base(bar)
        {
        }
    }
}";
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { fooCode, barCode, bazCode, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void ChainedPropertyOnInjected()
            {
                var fooCode = @"
namespace N
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
namespace N
{
    public class Bar
    {
    }
}";

                var bazCode = @"
namespace N
{
    public class Baz
    {
        public Baz(Bar bar)
        {
            this.Bar = bar;
        }

        public Bar Bar { get; }
    }
}";

                var before = @"
namespace N
{
    public class Meh : Foo
    {
        public Meh(Baz baz)
           : base(baz.↓Bar)
        {
        }
    }
}";

                var after = @"
namespace N
{
    public class Meh : Foo
    {
        public Meh(Baz baz, Bar bar)
           : base(bar)
        {
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { fooCode, barCode, bazCode, before }, after, fixTitle: "Inject safe.");
            }
        }
    }
}
