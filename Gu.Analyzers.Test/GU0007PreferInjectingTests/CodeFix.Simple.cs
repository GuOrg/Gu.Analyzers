namespace Gu.Analyzers.Test.GU0007PreferInjectingTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static partial class CodeFix
    {
        internal static class Simple
        {
            private const string Bar = @"
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
    public class C
    {
        public C()
        {
            this.Bar = ↓new Bar();
        }

        public Bar Bar { get; }
    }
}";

                var after = @"
namespace N
{
    public class C
    {
        public C(Bar bar)
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
                var before = @"
namespace N
{
    public class C
    {
        public C()
        {
            Bar = ↓new Bar();
        }

        public Bar Bar { get; }
    }
}";

                var after = @"
namespace N
{
    public class C
    {
        public C(Bar bar)
        {
            Bar = bar;
        }

        public Bar Bar { get; }
    }
}";
                var expectedDiagnostic = ExpectedDiagnostic.WithMessage("Prefer injecting Bar.");
                RoslynAssert.CodeFix(Analyzer, Fix, expectedDiagnostic, new[] { before, Bar }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenNotInjectingChained()
            {
                var c1 = @"
namespace N
{
    public class C1
    {
        private readonly Bar bar;

        public C1(Bar bar)
        {
            this.bar = bar;
        }
    }
}";

                var before = @"
namespace N
{
    public class C3 : C1
    {
        public C3()
           : base(↓new Bar())
        {
        }
    }
}";

                var after = @"
namespace N
{
    public class C3 : C1
    {
        public C3(Bar bar)
           : base(bar)
        {
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { c1, Bar, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenNotInjectingChainedNameCollision()
            {
                var fooCode = @"
namespace N
{
    public class C1
    {
        private readonly Bar bar;

        public C1(int value, Bar bar)
        {
            this.bar = bar;
        }
    }
}";

                var before = @"
namespace N
{
    public class C3 : C1
    {
        public C3(int bar)
           : base(bar, ↓new Bar())
        {
        }
    }
}";

                var after = @"
namespace N
{
    public class C3 : C1
    {
        public C3(int bar, Bar bar_)
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
    public class C
    {
        private readonly Bar bar;

        public C(int value = 1)
        {
            this.bar = ↓new Bar();
        }
    }
}";

                var after = @"
namespace N
{
    public class C
    {
        private readonly Bar bar;

        public C(Bar bar, int value = 1)
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
    public class C
    {
        private readonly Bar bar;

        public C(params int[] values)
        {
            this.bar = ↓new Bar();
        }
    }
}";

                var after = @"
namespace N
{
    public class C
    {
        private readonly Bar bar;

        public C(Bar bar, params int[] values)
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
    public class C1
    {
        private readonly C2<int> c2;

        public C1(C2<int> c2)
        {
            this.c2 = c2;
        }
    }
}";
                var barCode = @"
namespace N
{
    public class C2<T>
    {
    }
}";

                var before = @"
namespace N
{
    public class C3 : C1
    {
        public C3()
           : base(↓new C2<int>())
        {
        }
    }
}";

                var after = @"
namespace N
{
    public class C3 : C1
    {
        public C3(C2<int> c2)
           : base(c2)
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
    public sealed class C
    {
        private readonly IsModuleReady<CModule> isCReady;

        public C()
        {
            this.isCReady = ↓new IsModuleReady<CModule>();
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

    public class CModule : Module { }
}";

                var after = @"
namespace N
{
    public sealed class C
    {
        private readonly IsModuleReady<CModule> isCReady;

        public C(IsModuleReady<CModule> isCReady)
        {
            this.isCReady = isCReady;
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { before, moduleCode }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenNotInjectingChainedNewWithInjectedArgument()
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
                var c2 = @"
namespace N
{
    public class C2
    {
        private readonly Baz baz;

        public C2(Baz baz)
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
    public class C3 : C1
    {
        public C3(Baz baz)
           : base(↓new C2(baz))
        {
        }
    }
}";

                var after = @"
namespace N
{
    public class C3 : C1
    {
        public C3(Baz baz, C2 c2)
           : base(c2)
        {
        }
    }
}";
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { c1, c2, bazCode, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void ChainedPropertyOnInjected()
            {
                var fooCode = @"
namespace N
{
    public class C1
    {
        private readonly C2 c2;

        public C1(C2 c2)
        {
            this.c2 = c2;
        }
    }
}";
                var barCode = @"
namespace N
{
    public class C2
    {
    }
}";

                var bazCode = @"
namespace N
{
    public class Baz
    {
        public Baz(C2 c2)
        {
            this.C2 = c2;
        }

        public C2 C2 { get; }
    }
}";

                var before = @"
namespace N
{
    public class C3 : C1
    {
        public C3(Baz baz)
           : base(baz.↓C2)
        {
        }
    }
}";

                var after = @"
namespace N
{
    public class C3 : C1
    {
        public C3(Baz baz, C2 c2)
           : base(c2)
        {
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { fooCode, barCode, bazCode, before }, after, fixTitle: "Inject safe.");
            }
        }
    }
}
