namespace Gu.Analyzers.Test.GU0021CalculatedPropertyAllocatesTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class CodeFix : CodeFixVerifier<GU0021CalculatedPropertyAllocates, UseGetOnlyCodeFixProvider>
    {
        [Test]
        public async Task ExpressionBodyAllocatingReferenceTypeFromGetOnlyProperties()
        {
            var testCode = @"
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

        public Foo Bar ↓=> new Foo(this.A, this.B, this.C, this.D);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Calculated property allocates reference type.");

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
            this.Bar = new Foo(this.A, this.B, this.C, this.D);
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }

        public Foo Bar { get; }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task ExpressionBodyAllocatingReferenceTypeFromGetOnlyPropertiesUnderscoreNames()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            A = a;
            B = b;
            C = c;
            D = d;
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }

        public Foo Bar ↓=> new Foo(A, B, C, D);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Calculated property allocates reference type.");

            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            A = a;
            B = b;
            C = c;
            D = d;
            Bar = new Foo(A, B, C, D);
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }

        public Foo Bar { get; }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task GetBodyAllocatingReferenceTypeFromGetOnlyProperties()
        {
            var testCode = @"
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

        public Foo Bar
        { 
            get { ↓return new Foo(this.A, this.B, this.C, this.D); }
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Calculated property allocates reference type.");

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
            this.Bar = new Foo(this.A, this.B, this.C, this.D);
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }

        public Foo Bar { get; }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AllocatingReferenceTypeFromGetOnlyPropertiesNoThis()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            A = a;
            B = b;
            C = c;
            D = d;
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }

        public Foo Bar ↓=> new Foo(A, B, C, D);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Calculated property allocates reference type.");

            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            A = a;
            B = b;
            C = c;
            D = d;
            Bar = new Foo(A, B, C, D);
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }

        public Foo Bar { get; }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AllocatingReferenceTypeFromReadOnlyFields()
        {
            var testCode = @"
namespace RoslynSandbox
{
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

        public Foo Bar ↓=> new Foo(this.a, this.b, this.c, this.d);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Calculated property allocates reference type.");

            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
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
            this.Bar = new Foo(this.a, this.b, this.c, this.d);
        }

        public Foo Bar { get; }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AllocatingReferenceTypeFromReadOnlyFieldsUnderscore()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public readonly int _a;
        public readonly int _b;
        public readonly int _c;
        public readonly int _d;

        public Foo(int a, int b, int c, int d)
        {
            _a = a;
            _b = b;
            _c = c;
            _d = d;
        }

        public Foo Bar ↓=> new Foo(_a, _b, _c, _d);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Calculated property allocates reference type.");

            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public readonly int _a;
        public readonly int _b;
        public readonly int _c;
        public readonly int _d;

        public Foo(int a, int b, int c, int d)
        {
            _a = a;
            _b = b;
            _c = c;
            _d = d;
            Bar = new Foo(_a, _b, _c, _d);
        }

        public Foo Bar { get; }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AllocatingReferenceTypeEmptyCtor()
        {
            var testCode = @"
namespace RoslynSandbox
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
                               .WithMessage("Calculated property allocates reference type.");

            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
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
namespace RoslynSandbox
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
                               .WithMessage("Calculated property allocates reference type.");

            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
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
        public async Task AllocatingReferenceTypeMethodGroup()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo(Func<int> creator)
        {
            this.Value = creator();
        }
        
        public int Value { get; set; }

        public Foo Bar ↓=> new Foo(CreateNumber);

        private static int CreateNumber() => 2;
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Calculated property allocates reference type.");

            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo(Func<int> creator)
        {
            this.Value = creator();
            this.Bar = new Foo(CreateNumber);
        }
        
        public int Value { get; set; }

        public Foo Bar { get; }

        private static int CreateNumber() => 2;
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AllocatingReferenceTypeFromMutablePropertyNoFix1()
        {
            var testCode = @"
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

        public int D { get; set; }

        public Foo Bar ↓=> new Foo(this.A, this.B, this.C, this.D);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Calculated property allocates reference type.");

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
            this.Bar = new Foo(this.A, this.B, this.C, this.D);
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; set; }

        public Foo Bar { get; }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AllocatingReferenceTypeFromMutablePropertyNoFix2()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            A = a;
            B = b;
            C = c;
            D = d;
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; set; }

        public Foo Bar ↓=> new Foo(A, B, C, D);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Calculated property allocates reference type.");

            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            A = a;
            B = b;
            C = c;
            D = d;
            Bar = new Foo(A, B, C, D);
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; set; }

        public Foo Bar { get; }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AllocatingReferenceTypeFromMutableFieldNoFix()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public readonly int a;
        public readonly int b;
        public readonly int c;
        public int d;

        public Foo(int a, int b, int c, int d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        public Foo Bar ↓=> new Foo(this.a, this.b, this.c, this.d);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Calculated property allocates reference type.");

            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public readonly int a;
        public readonly int b;
        public readonly int c;
        public int d;

        public Foo(int a, int b, int c, int d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
            this.Bar = new Foo(this.a, this.b, this.c, this.d);
        }

        public Foo Bar { get; }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AllocatingReferenceTypeFromMutableFieldUnderscoreNoFix()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public readonly int _a;
        public readonly int _b;
        public readonly int _c;
        public int _d;

        public Foo(int a, int b, int c, int d)
        {
            _a = a;
            _b = b;
            _c = c;
            _d = d;
        }

        public Foo Bar ↓=> new Foo(_a, _b, _c, _d);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Calculated property allocates reference type.");

            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public readonly int _a;
        public readonly int _b;
        public readonly int _c;
        public int _d;

        public Foo(int a, int b, int c, int d)
        {
            _a = a;
            _b = b;
            _c = c;
            _d = d;
            Bar = new Foo(_a, _b, _c, _d);
        }

        public Foo Bar { get; }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AllocatingReferenceTypeFromMutableMembersObjectInitializerNoFix()
        {
            var testCode = @"
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

        public int D { get; set; }

        public Foo Bar ↓=> new Foo(this.A, this.B, this.C, 0)
            {
                D = this.D
            };
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Calculated property allocates reference type.");

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
            this.Bar = new Foo(this.A, this.B, this.C, 0)
            {
                D = this.D
            };
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; set; }

        public Foo Bar { get; }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AllocatingReferenceTypeFromSecondLevelNoFix1()
        {
            var testCode = @"
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
            this.Bar1 = new Foo(a, b, c, d);
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; set; }

        public Foo Bar1 { get; }
    
        public Foo Bar2 ↓=> new Foo(this.A, this.B, this.C, this.Bar1.D);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Calculated property allocates reference type.");

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
            this.Bar1 = new Foo(a, b, c, d);
            this.Bar2 = new Foo(this.A, this.B, this.C, this.Bar1.D);
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; set; }

        public Foo Bar1 { get; }
    
        public Foo Bar2 { get; }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AllocatingReferenceTypeFromSecondLevelNoFix2()
        {
            var testCode = @"
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
            this.Bar1 = new Foo(a, b, c, d);
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }

        public Foo Bar1 { get; set; }
    
        public Foo Bar2 ↓=> new Foo(this.A, this.B, this.C, this.Bar1.D);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Calculated property allocates reference type.");

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
            this.Bar1 = new Foo(a, b, c, d);
            this.Bar2 = new Foo(this.A, this.B, this.C, this.Bar1.D);
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }

        public Foo Bar1 { get; set; }
    
        public Foo Bar2 { get; }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}
