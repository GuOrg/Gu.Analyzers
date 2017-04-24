namespace Gu.Analyzers.Test.GU0007PreferInjectingTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        internal class Member : NestedCodeFixVerifier<CodeFix>
        {
            private static readonly string BarCode = @"
    public class Bar
    {
        public void Baz()
        {
        }
    }";

            private static readonly string LocatorCode = @"
    public class ServiceLocator
    {
        public ServiceLocator(Bar bar)
        {
            this.Bar = bar;
        }

        public Bar Bar { get; }
    }";

            [Test]
            public async Task WhenNotInjectingFieldInitialization()
            {
                var fooCode = @"
    public class Foo
    {
        private readonly Bar bar;

        public Foo(ServiceLocator locator)
        {
            this.bar = locator.↓Bar;
        }
    }";

                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref fooCode)
                                   .WithMessage("Prefer injecting.");
                await this.VerifyCSharpDiagnosticAsync(new[] { BarCode, LocatorCode, fooCode }, expected)
                          .ConfigureAwait(false);

                var fixedCode = @"
    public class Foo
    {
        private readonly Bar bar;

        public Foo(ServiceLocator locator, Bar bar)
        {
            this.bar = bar;
        }
    }";
                await this.VerifyCSharpFixAsync(new[] { BarCode, LocatorCode, fooCode }, new[] { BarCode, LocatorCode, fixedCode })
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task WhenUsingLocatorInMethod()
            {
                var fooCode = @"
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
    }";

                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref fooCode)
                                   .WithMessage("Prefer injecting.");
                await this.VerifyCSharpDiagnosticAsync(new[] { BarCode, LocatorCode, fooCode }, expected)
                          .ConfigureAwait(false);

                var fixedCode = @"
    public class Foo
    {
        private readonly ServiceLocator locator;
        private readonly Bar bar;

        public Foo(ServiceLocator locator, Bar bar)
        {
            this.bar = bar;
        }

        public void Meh()
        {
            this.bar.Baz();
        }
    }";
                await this.VerifyCSharpFixAsync(new[] { BarCode, LocatorCode, fooCode }, new[] { BarCode, LocatorCode, fixedCode })
                          .ConfigureAwait(false);
            }
        }
    }
}