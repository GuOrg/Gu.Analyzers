namespace Gu.Analyzers.Test.GU0007PreferInjectingTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class CodeFix : CodeFixVerifier<GU0007PreferInjecting, InjectCodeFixProvider>
    {
        internal class Simple : NestedCodeFixVerifier<CodeFix>
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
            public async Task WhenNotInjectingFieldInitialization()
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
                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref fooCode)
                                   .WithMessage("Prefer injecting.");
                await this.VerifyCSharpDiagnosticAsync(new[] { fooCode, BarCode }, expected)
                          .ConfigureAwait(false);

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
                await this.VerifyCSharpFixAsync(new[] { fooCode, BarCode }, new[] { fixedCode, BarCode })
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task WhenNotInjectingChained()
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
                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref mehCode)
                                   .WithMessage("Prefer injecting.");
                await this.VerifyCSharpDiagnosticAsync(new[] { fooCode, BarCode, mehCode }, expected)
                          .ConfigureAwait(false);

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
                await this.VerifyCSharpFixAsync(new[] { fooCode, BarCode, mehCode }, new[] { fooCode, BarCode, fixedCode })
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task WhenNotInjectingChainedNameCollision()
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
                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref testCode)
                                   .WithMessage("Prefer injecting.");
                await this.VerifyCSharpDiagnosticAsync(new[] { fooCode, BarCode, testCode }, expected)
                          .ConfigureAwait(false);

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
                await this.VerifyCSharpFixAsync(new[] { fooCode, BarCode, testCode }, new[] { fooCode, BarCode, fixedCode })
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task WhenNotInjectingOptional()
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
                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref testCode)
                                   .WithMessage("Prefer injecting.");
                await this.VerifyCSharpDiagnosticAsync(new[] { BarCode, testCode }, expected)
                          .ConfigureAwait(false);

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
                await this.VerifyCSharpFixAsync(new[] { BarCode, testCode }, new[] { BarCode, fixedCode })
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task WhenNotInjectingParams()
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
                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref testCode)
                                   .WithMessage("Prefer injecting.");
                await this.VerifyCSharpDiagnosticAsync(new[] { BarCode, testCode }, expected)
                          .ConfigureAwait(false);

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
                await this.VerifyCSharpFixAsync(new[] { BarCode, testCode }, new[] { BarCode, fixedCode })
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task WhenNotInjectingChainedGeneric()
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
                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref mehCode)
                                   .WithMessage("Prefer injecting.");
                await this.VerifyCSharpDiagnosticAsync(new[] { fooCode, barCode, mehCode }, expected)
                          .ConfigureAwait(false);

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
                await this.VerifyCSharpFixAsync(new[] { fooCode, barCode, mehCode }, new[] { fooCode, barCode, fixedCode })
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task WhenNotInjectingChainedGenericParameterName()
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

                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref fooCode)
                                   .WithMessage("Prefer injecting.");
                await this.VerifyCSharpDiagnosticAsync(new[] { fooCode, moduleCode }, expected)
                          .ConfigureAwait(false);

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
                await this.VerifyCSharpFixAsync(new[] { fooCode, moduleCode }, new[] { fixedCode, moduleCode })
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task WhenNotInjectingChainedNewWithInjectedArgument()
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
    public class Baz
    {
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
                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref testCode)
                                   .WithMessage("Prefer injecting.");
                await this.VerifyCSharpDiagnosticAsync(new[] { fooCode, barCode, bazCode, testCode }, expected)
                          .ConfigureAwait(false);

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
                await this.VerifyCSharpFixAsync(new[] { fooCode, barCode, bazCode, testCode }, new[] { fooCode, barCode, bazCode, fixedCode })
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task UnsafeWhenNotInjectingChainedPassingInPropertyOfInjected()
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
                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref mehCode)
                                   .WithMessage("Prefer injecting.");
                await this.VerifyCSharpDiagnosticAsync(new[] { fooCode, barCode, bazCode, mehCode }, expected)
                          .ConfigureAwait(false);

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
                await this.VerifyCSharpFixAsync(new[] { fooCode, barCode, bazCode, mehCode }, new[] { fooCode, barCode, bazCode, fixedCode })
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task UnsafeWhenNotInjectingChainedPropertyOnInjected()
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
                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref mehCode)
                                   .WithMessage("Prefer injecting.");
                await this.VerifyCSharpDiagnosticAsync(new[] { fooCode, barCode, bazCode, mehCode }, expected)
                          .ConfigureAwait(false);

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
                await this.VerifyCSharpFixAsync(new[] { fooCode, barCode, bazCode, mehCode }, new[] { fooCode, barCode, bazCode, fixedCode })
                          .ConfigureAwait(false);
            }
        }
    }
}
