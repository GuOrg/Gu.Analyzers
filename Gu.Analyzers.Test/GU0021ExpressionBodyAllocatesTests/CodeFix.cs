namespace Gu.Analyzers.Test.GU0021ExpressionBodyAllocatesTests
{
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class CodeFix : CodeFixVerifier<GU0021ExpressionBodyAllocates, UseGetOnlyCodeFixProvider>
    {
        [Test]
        public async Task AllocatingReferenceTypeFromImmutableMembers()
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
    }
}
