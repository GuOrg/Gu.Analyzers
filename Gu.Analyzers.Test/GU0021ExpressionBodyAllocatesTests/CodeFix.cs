namespace Gu.Analyzers.Test.GU0021ExpressionBodyAllocatesTests
{
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class CodeFix : CodeFixVerifier<GU0021ExpressionBodyAllocates, UseGetOnlyCodeFixProvider>
    {
        [Test]
        public async Task AllocatingReferenceTypeFromGetOnlyProperties()
        {
            var testCode = @"
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

    public Foo Bar ↓=> new Foo(this.A, this.B, this.C, this.D);
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Expression body allocates reference type.");

            await this.VerifyCSharpDiagnosticAsync(testCode, new[] { expected }, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
public class Foo
{
    public Foo(int a, int b, int c, int d)
    {
        this.A = a;
        this.B = b;
        this.C = c;
        this.D = d;
        this.Bar = new Foo(this.A, this.B, this.C, this.D);
    }

    public int A { get; }

    public int B { get; }

    public int C { get; }

    public int D { get; }

    public Foo Bar { get; }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AllocatingReferenceTypeFromReadOnlyFields()
        {
            var testCode = @"
public class Foo
{
    public readonly int a;
    public readonly int b;
    public readonly int c;
    public readonly int d;

    public Foo(int a, int b, int c, int d)
    {
        this.a = a;
        this.b = b;
        this.c = c;
        this.d = d;
    }

    public Foo Bar ↓=> new Foo(this.a, this.b, this.b, this.d);
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Expression body allocates reference type.");

            await this.VerifyCSharpDiagnosticAsync(testCode, new[] { expected }, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
public class Foo
{
    public readonly int a;
    public readonly int b;
    public readonly int c;
    public readonly int d;

    public Foo(int a, int b, int c, int d)
    {
        this.a = a;
        this.b = b;
        this.c = c;
        this.d = d;
        this.Bar = new Foo(this.a, this.b, this.b, this.d);
    }

    public Foo Bar { get; }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AllocatingReferenceTypeEmptyCtor()
        {
            var testCode = @"
namespace Test
{
    public class Foo
    {
        public Foo()
        {
        }

        public Foo Bar ↓=> new Foo();
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Expression body allocates reference type.");

            await this.VerifyCSharpDiagnosticAsync(testCode, new[] { expected }, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
namespace Test
{
    public class Foo
    {
        public Foo()
        {
            this.Bar = new Foo();
        }

        public Foo Bar { get; }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AllocatingReferenceTypeLambdaUsingMutableCtor()
        {
            var testCode = @"
namespace Test
{
    using System;

    public class Foo
    {
        public Foo(Func<int> creator)
        {
            this.Value = creator();
        }
        
        public int Value { get; set; }

        public Foo Bar ↓=> new Foo(() => this.Value);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Expression body allocates reference type.");

            await this.VerifyCSharpDiagnosticAsync(testCode, new[] { expected }, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
namespace Test
{
    using System;

    public class Foo
    {
        public Foo(Func<int> creator)
        {
            this.Value = creator();
            this.Bar = new Foo(() => this.Value);
        }
        
        public int Value { get; set; }

        public Foo Bar { get; }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AllocatingReferenceTypeFromMutableMembersNoFix()
        {
            var testCode = @"
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

    public int D { get; set; }

    public Foo Bar ↓=> new Foo(this.A, this.B, this.C, this.D);
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Expression body allocates reference type.");

            await this.VerifyCSharpDiagnosticAsync(testCode, new[] { expected }, CancellationToken.None).ConfigureAwait(false);

            await this.VerifyCSharpFixAsync(testCode, testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AllocatingReferenceTypeFromMutableMembersObjectInitializerNoFix()
        {
            var testCode = @"
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

    public int D { get; set; }

    public Foo Bar ↓=> new Foo(this.A, this.B, this.C, 0)
        {
            D = this.D
        };
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Expression body allocates reference type.");

            await this.VerifyCSharpDiagnosticAsync(testCode, new[] { expected }, CancellationToken.None).ConfigureAwait(false);

            await this.VerifyCSharpFixAsync(testCode, testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AllocatingReferenceTypeFromSecondLevelNoFix1()
        {
            var testCode = @"
public class Foo
{
    public Foo(int a, int b, int c, int d)
    {
        this.A = a;
        this.B = b;
        this.C = c;
        this.D = d;
        this.Bar1 = new Foo(a, b, c, d);
    }

    public int A { get; }

    public int B { get; }

    public int C { get; }

    public int D { get; set; }

    public Foo Bar1 { get; }
    
    public Foo Bar2 ↓=> new Foo(this.A, this.B, this.C, this.Bar1.D);
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Expression body allocates reference type.");

            await this.VerifyCSharpDiagnosticAsync(testCode, new[] { expected }, CancellationToken.None).ConfigureAwait(false);

            await this.VerifyCSharpFixAsync(testCode, testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AllocatingReferenceTypeFromSecondLevelNoFix2()
        {
            var testCode = @"
public class Foo
{
    public Foo(int a, int b, int c, int d)
    {
        this.A = a;
        this.B = b;
        this.C = c;
        this.D = d;
        this.Bar1 = new Foo(a, b, c, d);
    }

    public int A { get; }

    public int B { get; }

    public int C { get; }

    public int D { get; }

    public Foo Bar1 { get; set; }
    
    public Foo Bar2 ↓=> new Foo(this.A, this.B, this.C, this.Bar1.D);
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Expression body allocates reference type.");

            await this.VerifyCSharpDiagnosticAsync(testCode, new[] { expected }, CancellationToken.None).ConfigureAwait(false);

            await this.VerifyCSharpFixAsync(testCode, testCode).ConfigureAwait(false);
        }
    }
}
