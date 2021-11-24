namespace Gu.Analyzers.Test.GU0007PreferInjectingTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static partial class CodeFix
    {
        internal static class Simple
        {
            private const string C1 = @"
namespace N
{
    public class C1
    {
        public void M()
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
    public class C
    {
        private readonly C1 f;

        public C()
        {
            this.f = ↓new C1();
        }
    }
}";

                var after = @"
namespace N
{
    public class C
    {
        private readonly C1 f;

        public C(C1 f)
        {
            this.f = f;
        }
    }
}";
                RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic.WithMessage("Prefer injecting C1"), before, C1);
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { before, C1 }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenAssigningUnderscoreFieldWithObjectCreation()
            {
                var before = @"
namespace N
{
    public class C
    {
        private readonly C1 _f;

        public C()
        {
            _f = ↓new C1();
        }
    }
}";

                var after = @"
namespace N
{
    public class C
    {
        private readonly C1 _f;

        public C(C1 f)
        {
            _f = f;
        }
    }
}";
                var expectedDiagnostic = ExpectedDiagnostic.WithMessage("Prefer injecting C1");
                RoslynAssert.CodeFix(Analyzer, Fix, expectedDiagnostic, new[] { before, C1 }, after, fixTitle: "Inject safe.");
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
            this.P = ↓new C1();
        }

        public C1 P { get; }
    }
}";

                var after = @"
namespace N
{
    public class C
    {
        public C(C1 p)
        {
            this.P = p;
        }

        public C1 P { get; }
    }
}";
                var expectedDiagnostic = ExpectedDiagnostic.WithMessage("Prefer injecting C1");
                RoslynAssert.CodeFix(Analyzer, Fix, expectedDiagnostic, new[] { before, C1 }, after, fixTitle: "Inject safe.");
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
            P = ↓new C1();
        }

        public C1 P { get; }
    }
}";

                var after = @"
namespace N
{
    public class C
    {
        public C(C1 p)
        {
            P = p;
        }

        public C1 P { get; }
    }
}";
                var expectedDiagnostic = ExpectedDiagnostic.WithMessage("Prefer injecting C1");
                RoslynAssert.CodeFix(Analyzer, Fix, expectedDiagnostic, new[] { before, C1 }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenNotInjectingChained()
            {
                var c1 = @"
namespace N
{
    public class C2
    {
        private readonly C1 f;

        public C2(C1 f)
        {
            this.f = f;
        }
    }
}";

                var before = @"
namespace N
{
    public class C3 : C2
    {
        public C3()
           : base(↓new C1())
        {
        }
    }
}";

                var after = @"
namespace N
{
    public class C3 : C2
    {
        public C3(C1 c1)
           : base(c1)
        {
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { c1, C1, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenNotInjectingChainedNameCollision()
            {
                var c2 = @"
namespace N
{
    public class C2
    {
        private readonly C1 f;

        public C2(int value, C1 f)
        {
            this.f = f;
        }
    }
}";

                var before = @"
namespace N
{
    public class C3 : C2
    {
        public C3(int c1)
           : base(c1, ↓new C1())
        {
        }
    }
}";

                var after = @"
namespace N
{
    public class C3 : C2
    {
        public C3(int c1, C1 c1_)
           : base(c1, c1_)
        {
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { c2, C1, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenNotInjectingOptional()
            {
                var before = @"
namespace N
{
    public class C
    {
        private readonly C1 f;

        public C(int value = 1)
        {
            this.f = ↓new C1();
        }
    }
}";

                var after = @"
namespace N
{
    public class C
    {
        private readonly C1 f;

        public C(C1 f, int value = 1)
        {
            this.f = f;
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { C1, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenNotInjectingParams()
            {
                var before = @"
namespace N
{
    public class C
    {
        private readonly C1 f;

        public C(params int[] values)
        {
            this.f = ↓new C1();
        }
    }
}";

                var after = @"
namespace N
{
    public class C
    {
        private readonly C1 f;

        public C(C1 f, params int[] values)
        {
            this.f = f;
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { C1, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void WhenNotInjectingChainedGeneric()
            {
                var c2 = @"
namespace N
{
    public class C2
    {
        private readonly C3<int> f;

        public C2(C3<int> f)
        {
            this.f = f;
        }
    }
}";
                var c3 = @"
namespace N
{
    public class C3<T>
    {
    }
}";

                var before = @"
namespace N
{
    public class C4 : C2
    {
        public C4()
           : base(↓new C3<int>())
        {
        }
    }
}";

                var after = @"
namespace N
{
    public class C4 : C2
    {
        public C4(C3<int> c3)
           : base(c3)
        {
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { c2, c3, before }, after, fixTitle: "Inject safe.");
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
    public class C2
    {
        private readonly C3 f;

        public C2(C3 f)
        {
            this.f = f;
        }
    }
}";
                var c2 = @"
namespace N
{
    public class C3
    {
        private readonly C4 f;

        public C3(C4 f)
        {
            this.f = f;
        }
    }
}";

                var c4 = @"
namespace N
{
    public class C4
    {
    }
}";
                var before = @"
namespace N
{
    public class C5 : C2
    {
        public C5(C4 c4)
           : base(↓new C3(c4))
        {
        }
    }
}";

                var after = @"
namespace N
{
    public class C5 : C2
    {
        public C5(C4 c4, C3 c3)
           : base(c3)
        {
        }
    }
}";
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { c1, c2, c4, before }, after, fixTitle: "Inject safe.");
            }

            [Test]
            public static void ChainedPropertyOnInjected()
            {
                var c1 = @"
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
                var c2 = @"
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { c1, c2, bazCode, before }, after, fixTitle: "Inject safe.");
            }
        }
    }
}
