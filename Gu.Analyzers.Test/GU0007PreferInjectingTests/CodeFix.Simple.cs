namespace Gu.Analyzers.Test.GU0007PreferInjectingTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        internal class Simple
        {
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

            [Test]
            public void WhenNotInjectingFieldInitialization()
            {
                var fooCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { fooCode, BarCode }, fixedCode);
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

                var mehCode = @"
namespace RoslynSandbox
{
    public class Meh : Foo
    {
        public Meh()
           : base(↓new Bar())
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    public class Meh : Foo
    {
        public Meh(Bar bar)
           : base(bar)
        {
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { fooCode, BarCode, mehCode }, fixedCode);
            }

            [Test]
            public void WhenNotInjectingChainedNameCollision()
            {
                var fooCode = @"
namespace RoslynSandbox
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

                var testCode = @"
namespace RoslynSandbox
{
    public class Meh : Foo
    {
        public Meh(int bar)
           : base(bar, ↓new Bar())
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    public class Meh : Foo
    {
        public Meh(int bar, Bar bar_)
           : base(bar, bar_)
        {
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { fooCode, BarCode, testCode }, fixedCode);
            }

            [Test]
            public void WhenNotInjectingOptional()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { BarCode, testCode }, fixedCode);
            }

            [Test]
            public void WhenNotInjectingParams()
            {
                var testCode = @"
namespace RoslynSandbox
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

                var fixedCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { BarCode, testCode }, fixedCode);
            }

            [Test]
            public void WhenNotInjectingChainedGeneric()
            {
                var fooCode = @"
namespace RoslynSandbox
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
namespace RoslynSandbox
{
    public class Bar<T>
    {
    }
}";

                var mehCode = @"
namespace RoslynSandbox
{
    public class Meh : Foo
    {
        public Meh()
           : base(↓new Bar<int>())
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    public class Meh : Foo
    {
        public Meh(Bar<int> bar)
           : base(bar)
        {
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { fooCode, barCode, mehCode }, fixedCode);
            }

            [Test]
            public void WhenNotInjectingChainedGenericParameterName()
            {
                var fooCode = @"
namespace RoslynSandbox
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
namespace RoslynSandbox
{
    public class IsModuleReady<TModule>
        where TModule : Module
    {
    }

    public abstract class Module { }

    public class FooModule : Module { }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        private readonly IsModuleReady<FooModule> isFooReady;

        public Foo(IsModuleReady<FooModule> isFooModuleReady)
        {
            this.isFooReady = isFooModuleReady;
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { fooCode, moduleCode }, fixedCode);
            }

            [Test]
            public void WhenNotInjectingChainedNewWithInjectedArgument()
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
        private readonly Baz baz;

        public Bar(Baz baz)
        {
            this.baz = baz;
        }
    }
}";

                var bazCode = @"
namespace RoslynSandbox
{
    public class Baz
    {
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    public class Meh : Foo
    {
        public Meh(Baz baz)
           : base(↓new Bar(baz))
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    public class Meh : Foo
    {
        public Meh(Baz baz, Bar bar)
           : base(bar)
        {
        }
    }
}";
                AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { fooCode, barCode, bazCode, testCode }, fixedCode);
            }

            [Test]
            public void UnsafeWhenNotInjectingChainedPassingInPropertyOfInjected()
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

                var bazCode = @"
namespace RoslynSandbox
{
    public class Baz
    {
        public int Value { get; } = 2;
    }
}";

                var mehCode = @"
namespace RoslynSandbox
{
    public class Meh : Foo
    {
        public Meh(Baz baz)
           : base(↓new Bar(baz.Value))
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    public class Meh : Foo
    {
        public Meh(Baz baz, Bar bar)
           : base(bar)
        {
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { fooCode, barCode, bazCode, mehCode }, fixedCode);
            }

            [Test]
            public void UnsafeWhenNotInjectingChainedPropertyOnInjected()
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
    }
}";

                var bazCode = @"
namespace RoslynSandbox
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

                var mehCode = @"
namespace RoslynSandbox
{
    public class Meh : Foo
    {
        public Meh(Baz baz)
           : base(baz.↓Bar)
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    public class Meh : Foo
    {
        public Meh(Baz baz, Bar bar)
           : base(bar)
        {
        }
    }
}";
                AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { fooCode, barCode, bazCode, mehCode }, fixedCode);
            }
        }
    }
}
