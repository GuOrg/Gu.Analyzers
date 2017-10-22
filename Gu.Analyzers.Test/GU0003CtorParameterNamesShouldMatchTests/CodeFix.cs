namespace Gu.Analyzers.Test.GU0003CtorParameterNamesShouldMatchTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class CodeFix : CodeFixVerifier<GU0003CtorParameterNamesShouldMatch, RenameConstructorArgumentsCodeFixProvider>
    {
        [Test]
        public async Task ConstructorSettingProperties()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int ↓a1, int b, int c, int d)
        {
            this.A = a1;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Name the parameters to match the members.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task ChainedConstructorSettingProperties()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int ↓a1, int b, int c)
            : this(a1, b, c, 1)
        {
        }

        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Name the parameters to match the members.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a, int b, int c)
            : this(a, b, c, 1)
        {
        }

        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task BaseConstructorCall()
        {
            var fooCode = @"
namespace RoslynSandbox
{
    public class Bar : Foo
    {
        public Bar(int ↓a1, int b, int c, int d)
            : base(a1, b, c, d)
        {
        }
    }
}";
            var barCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref fooCode)
                               .WithMessage("Name the parameters to match the members.");
            await this.VerifyCSharpDiagnosticAsync(new[] { fooCode, barCode }, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    public class Bar : Foo
    {
        public Bar(int a, int b, int c, int d)
            : base(a, b, c, d)
        {
        }
    }
}";
            await this.VerifyCSharpFixAsync(new[] { fooCode, barCode }, new[] { fixedCode, barCode }).ConfigureAwait(false);
        }

        [Test]
        public async Task ConstructorSettingFields()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int a;
        private readonly int b;
        private readonly int c;
        private readonly int d;

        public Foo(int ↓a1, int b, int c, int d)
        {
            this.a = a1;
            this.b = b;
            this.c = c;
            this.d = d;
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Name the parameters to match the members.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int a;
        private readonly int b;
        private readonly int c;
        private readonly int d;

        public Foo(int a, int b, int c, int d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task ConstructorSettingFieldsPrefixedWithUnderscore()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int _a;
        private readonly int _b;
        private readonly int _c;
        private readonly int _d;

        public Foo(int ↓a1, int b, int c, int d)
        {
            _a = a1;
            _b = b;
            _c = c;
            _d = d;
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Name the parameters to match the members.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int _a;
        private readonly int _b;
        private readonly int _c;
        private readonly int _d;

        public Foo(int a, int b, int c, int d)
        {
            _a = a;
            _b = b;
            _c = c;
            _d = d;
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoresWhenBaseIsParams()
        {
            var fooCode = @"
namespace RoslynSandbox
{
    public class Bar : Foo
    {
        public Bar(int ↓a1, int b, int c, int d)
            : base(a1, b, c, d)
        {
        }
    }
}";
            var barCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a, params int[] values)
        {
            this.A = a;
            this.Values = values;
        }

        public int A { get; }

        public int[] Values { get; }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref fooCode)
                               .WithMessage("Name the parameters to match the members.");
            await this.VerifyCSharpDiagnosticAsync(new[] { fooCode, barCode }, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    public class Bar : Foo
    {
        public Bar(int a, int b, int c, int d)
            : base(a, b, c, d)
        {
        }
    }
}";
            await this.VerifyCSharpFixAsync(new[] { fooCode, barCode }, new[] { fixedCode, barCode }).ConfigureAwait(false);
        }
    }
}
