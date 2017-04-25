namespace Gu.Analyzers.Test.GU0007PreferInjectingTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class CodeFix : CodeFixVerifier<GU0007PreferInjecting, InjectCodeFixProvider>
    {
        internal class Member : NestedCodeFixVerifier<CodeFix>
        {
            private static readonly string BarCode = @"
namespace RoslynSandbox
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

            private static readonly string FooBaseCode = @"
namespace RoslynSandbox
{
    public abstract class FooBase
    {
        private readonly Bar bar;

        protected FooBase(Bar bar)
        {
            this.bar = bar;
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
        }

        public Bar Bar { get; }

        public object BarObject { get; }
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

        public Foo(ServiceLocator locator)
        {
            this.bar = locator.↓Bar;
        }
    }
}";

                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref fooCode)
                                   .WithMessage("Prefer injecting.");
                await this.VerifyCSharpDiagnosticAsync(new[] { BarCode, LocatorCode, fooCode }, expected)
                          .ConfigureAwait(false);

                var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly Bar bar;

        public Foo(ServiceLocator locator, Bar bar)
        {
            this.bar = bar;
        }
    }
}";
                await this.VerifyCSharpFixAsync(new[] { BarCode, LocatorCode, fooCode }, new[] { BarCode, LocatorCode, fixedCode })
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task WhenNotInjectingFieldInitializationUnderscore()
            {
                var fooCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly Bar _bar;

        public Foo(ServiceLocator locator)
        {
            _bar = locator.↓Bar;
        }
    }
}";

                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref fooCode)
                                   .WithMessage("Prefer injecting.");
                await this.VerifyCSharpDiagnosticAsync(new[] { BarCode, LocatorCode, fooCode }, expected)
                          .ConfigureAwait(false);

                var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly Bar _bar;

        public Foo(ServiceLocator locator, Bar bar)
        {
            _bar = bar;
        }
    }
}";
                await this.VerifyCSharpFixAsync(new[] { BarCode, LocatorCode, fooCode }, new[] { BarCode, LocatorCode, fixedCode })
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task WhenNotInjectingFieldInitializationObject()
            {
                var fooCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly Bar bar;

        public Foo(ServiceLocator locator)
        {
            this.bar = locator.↓BarObject;
        }
    }
}";

                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref fooCode)
                                   .WithMessage("Prefer injecting.");
                await this.VerifyCSharpDiagnosticAsync(new[] { BarCode, LocatorCode, fooCode }, expected)
                          .ConfigureAwait(false);

                var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly Bar bar;

        public Foo(ServiceLocator locator, Bar bar)
        {
            this.bar = bar;
        }
    }
}";
                await this.VerifyCSharpFixAsync(new[] { BarCode, LocatorCode, fooCode }, new[] { BarCode, LocatorCode, fixedCode })
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task WhenNotInjectingFieldInitializationWithNameCollision()
            {
                var enumCode = @"
namespace RoslynSandbox
{
    public enum Meh
    {
        Bar
    }
}";
                var fooCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly Bar bar;

        public Foo(ServiceLocator locator, Meh meh)
        {
            this.bar = locator.↓Bar;
            if (meh == Meh.Bar)
            {
            }
        }
    }
}";

                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref fooCode)
                                   .WithMessage("Prefer injecting.");
                await this.VerifyCSharpDiagnosticAsync(new[] { BarCode, LocatorCode, enumCode, fooCode }, expected)
                          .ConfigureAwait(false);

                var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly Bar bar;

        public Foo(ServiceLocator locator, Meh meh, Bar bar)
        {
            this.bar = bar;
            if (meh == Meh.Bar)
            {
            }
        }
    }
}";
                await this.VerifyCSharpFixAsync(new[] { BarCode, LocatorCode, enumCode, fooCode }, new[] { BarCode, LocatorCode, enumCode, fixedCode })
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task FieldInitializationAndBaseCall()
            {
                var fooCode = @"
namespace RoslynSandbox
{
    public class Foo : FooBase
    {
        private readonly Bar bar;

        public Foo(ServiceLocator locator)
            : base(locator.Bar)
        {
            this.bar = locator.Bar;
        }
    }
}";

                var expected1 = this.CSharpDiagnostic()
                                   .WithLocation("Foo.cs", 9, 28)
                                   .WithMessage("Prefer injecting.");
                var expected2 = this.CSharpDiagnostic()
                                    .WithLocation("Foo.cs", 11, 32)
                                    .WithMessage("Prefer injecting.");
                await this.VerifyCSharpDiagnosticAsync(new[] { BarCode, LocatorCode, FooBaseCode, fooCode }, new[] { expected1, expected2 })
                          .ConfigureAwait(false);

                var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo : FooBase
    {
        private readonly Bar bar;

        public Foo(ServiceLocator locator, Bar bar)
            : base(bar)
        {
            this.bar = bar;
        }
    }
}";
                await this.VerifyCSharpFixAsync(new[] { BarCode, LocatorCode, fooCode }, new[] { BarCode, LocatorCode, fixedCode })
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task FieldInitializationAndBaseCallUnderscoreNames()
            {
                var fooCode = @"
namespace RoslynSandbox
{
    public class Foo : FooBase
    {
        private readonly Bar _bar;

        public Foo(ServiceLocator locator)
            : base(locator.Bar)
        {
            _bar = locator.Bar;
        }
    }
}";

                var expected1 = this.CSharpDiagnostic()
                                    .WithLocation("Foo.cs", 9, 28)
                                    .WithMessage("Prefer injecting.");
                var expected2 = this.CSharpDiagnostic()
                                    .WithLocation("Foo.cs", 11, 28)
                                    .WithMessage("Prefer injecting.");
                await this.VerifyCSharpDiagnosticAsync(new[] { BarCode, LocatorCode, FooBaseCode, fooCode }, new[] { expected1, expected2 })
                          .ConfigureAwait(false);

                var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo : FooBase
    {
        private readonly Bar _bar;

        public Foo(ServiceLocator locator, Bar bar)
            : base(bar)
        {
            _bar = bar;
        }
    }
}";
                await this.VerifyCSharpFixAsync(new[] { BarCode, LocatorCode, fooCode }, new[] { BarCode, LocatorCode, fixedCode })
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task WhenUsingLocatorInMethod()
            {
                var fooCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly ServiceLocator locator;

        public Foo(ServiceLocator locator)
        {
            this.locator = locator;
        }

        public void Meh()
        {
            this.locator.↓Bar.Baz();
        }
    }
}";

                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref fooCode)
                                   .WithMessage("Prefer injecting.");
                await this.VerifyCSharpDiagnosticAsync(new[] { BarCode, LocatorCode, fooCode }, expected)
                          .ConfigureAwait(false);

                var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly ServiceLocator locator;
        private readonly Bar bar;

        public Foo(ServiceLocator locator, Bar bar)
        {
            this.locator = locator;
            this.bar = bar;
        }

        public void Meh()
        {
            this.bar.Baz();
        }
    }
}";
                await this.VerifyCSharpFixAsync(new[] { BarCode, LocatorCode, fooCode }, new[] { BarCode, LocatorCode, fixedCode })
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task WhenUsingLocatorInTwoMethods()
            {
                var fooCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly ServiceLocator locator;

        public Foo(ServiceLocator locator)
        {
            this.locator = locator;
        }

        public void Meh1()
        {
            this.locator.Bar.Baz();
        }

        public void Meh2()
        {
            this.locator.Bar.Baz(2);
        }
    }
}";

                var expected1 = this.CSharpDiagnostic()
                                    .WithLocation("Foo.cs", 15, 26)
                                    .WithMessage("Prefer injecting.");
                var expected2 = this.CSharpDiagnostic()
                                    .WithLocation("Foo.cs", 20, 26)
                                    .WithMessage("Prefer injecting.");
                await this.VerifyCSharpDiagnosticAsync(new[] { BarCode, LocatorCode, fooCode }, new[] { expected1, expected2 })
                          .ConfigureAwait(false);

                var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly ServiceLocator locator;
        private readonly Bar bar;

        public Foo(ServiceLocator locator, Bar bar)
        {
            this.locator = locator;
            this.bar = bar;
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
                await this.VerifyCSharpFixAsync(new[] { BarCode, LocatorCode, fooCode }, new[] { BarCode, LocatorCode, fixedCode })
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task WhenUsingLocatorInMethodUnderscoreNames()
            {
                var fooCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly ServiceLocator _locator;

        public Foo(ServiceLocator locator)
        {
            _locator = locator;
        }

        public void Meh()
        {
            _locator.↓Bar.Baz();
        }
    }
}";

                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref fooCode)
                                   .WithMessage("Prefer injecting.");
                await this.VerifyCSharpDiagnosticAsync(new[] { BarCode, LocatorCode, fooCode }, expected)
                          .ConfigureAwait(false);

                var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly ServiceLocator _locator;
        private readonly Bar _bar;

        public Foo(ServiceLocator locator, Bar bar)
        {
            _locator = locator;
            _bar = bar;
        }

        public void Meh()
        {
            _bar.Baz();
        }
    }
}";
                await this.VerifyCSharpFixAsync(new[] { BarCode, LocatorCode, fooCode }, new[] { BarCode, LocatorCode, fixedCode })
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task WhenUsingLocatorInMethodAndBaseCall()
            {
                var fooCode = @"
namespace RoslynSandbox
{
    public class Foo : FooBase
    {
        private readonly ServiceLocator locator;

        public Foo(ServiceLocator locator)
            : base(locator.Bar)
        {
            this.locator = locator;
        }

        public void Meh()
        {
            this.locator.Bar.Baz();
        }
    }
}";

                var expected1 = this.CSharpDiagnostic()
                                    .WithLocation("Foo.cs", 9, 28)
                                    .WithMessage("Prefer injecting.");
                var expected2 = this.CSharpDiagnostic()
                                    .WithLocation("Foo.cs", 16, 26)
                                    .WithMessage("Prefer injecting.");
                await this.VerifyCSharpDiagnosticAsync(new[] { BarCode, LocatorCode, FooBaseCode, fooCode }, new[] { expected1, expected2 })
                          .ConfigureAwait(false);

                var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo : FooBase
    {
        private readonly ServiceLocator locator;
        private readonly Bar bar;

        public Foo(ServiceLocator locator, Bar bar)
            : base(bar)
        {
            this.locator = locator;
            this.bar = bar;
        }

        public void Meh()
        {
            this.bar.Baz();
        }
    }
}";
                await this.VerifyCSharpFixAsync(new[] { BarCode, LocatorCode, FooBaseCode, fooCode }, new[] { BarCode, LocatorCode, FooBaseCode, fixedCode })
                          .ConfigureAwait(false);
            }
        }
    }
}