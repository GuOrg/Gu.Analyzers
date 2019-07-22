namespace Gu.Analyzers.Test.GU0021CalculatedPropertyAllocatesTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class CodeFix
    {
        private static readonly PropertyDeclarationAnalyzer Analyzer = new PropertyDeclarationAnalyzer();
        private static readonly UseGetOnlyFix Fix = new UseGetOnlyFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0021CalculatedPropertyAllocates);

        [Test]
        public static void ExpressionBodyAllocatingReferenceTypeFromGetOnlyProperties()
        {
            var before = @"
namespace N
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

        public Foo Bar => ↓new Foo(this.A, this.B, this.C, this.D);
    }
}";

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void ExpressionBodyAllocatingReferenceTypeFromGetOnlyPropertiesUnderscoreNames()
        {
            var before = @"
namespace N
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

        public Foo Bar => ↓new Foo(A, B, C, D);
    }
}";

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void GetBodyAllocatingReferenceTypeFromGetOnlyProperties()
        {
            var before = @"
namespace N
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
            get { return ↓new Foo(this.A, this.B, this.C, this.D); }
        }
    }
}";

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void AllocatingReferenceTypeFromGetOnlyPropertiesNoThis()
        {
            var before = @"
namespace N
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

        public Foo Bar => ↓new Foo(A, B, C, D);
    }
}";

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void AllocatingReferenceTypeFromReadOnlyFields()
        {
            var before = @"
namespace N
{
    public class C
    {
        public readonly int a;
        public readonly int b;
        public readonly int c;
        public readonly int d;

        public C(int a, int b, int c, int d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        public C P => ↓new C(this.a, this.b, this.c, this.d);
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        public readonly int a;
        public readonly int b;
        public readonly int c;
        public readonly int d;

        public C(int a, int b, int c, int d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
            this.P = new C(this.a, this.b, this.c, this.d);
        }

        public C P { get; }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void AllocatingReferenceTypeFromReadOnlyFieldsUnderscore()
        {
            var before = @"
namespace N
{
    public class C
    {
        public readonly int _a;
        public readonly int _b;
        public readonly int _c;
        public readonly int _d;

        public C(int a, int b, int c, int d)
        {
            _a = a;
            _b = b;
            _c = c;
            _d = d;
        }

        public C P => ↓new C(_a, _b, _c, _d);
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        public readonly int _a;
        public readonly int _b;
        public readonly int _c;
        public readonly int _d;

        public C(int a, int b, int c, int d)
        {
            _a = a;
            _b = b;
            _c = c;
            _d = d;
            P = new C(_a, _b, _c, _d);
        }

        public C P { get; }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void AllocatingReferenceTypeEmptyCtor()
        {
            var before = @"
namespace N
{
    public class C
    {
        public C()
        {
        }

        public C P => ↓new C();
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        public C()
        {
            this.P = new C();
        }

        public C P { get; }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void AllocatingReferenceTypeLambdaUsingMutableCtor()
        {
            var before = @"
namespace N
{
    using System;

    public class C
    {
        public C(Func<int> creator)
        {
            this.Value = creator();
        }
        
        public int Value { get; set; }

        public C P => ↓new C(() => this.Value);
    }
}";

            var after = @"
namespace N
{
    using System;

    public class C
    {
        public C(Func<int> creator)
        {
            this.Value = creator();
            this.P = new C(() => this.Value);
        }
        
        public int Value { get; set; }

        public C P { get; }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void AllocatingReferenceTypeMethodGroup()
        {
            var before = @"
namespace N
{
    using System;

    public class C
    {
        public C(Func<int> creator)
        {
            this.Value = creator();
        }
        
        public int Value { get; set; }

        public C P => ↓new C(CreateNumber);

        private static int CreateNumber() => 2;
    }
}";

            var after = @"
namespace N
{
    using System;

    public class C
    {
        public C(Func<int> creator)
        {
            this.Value = creator();
            this.P = new C(CreateNumber);
        }
        
        public int Value { get; set; }

        public C P { get; }

        private static int CreateNumber() => 2;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void AllocatingReferenceTypeFromMutablePropertyNoFix1()
        {
            var before = @"
namespace N
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

        public Foo Bar => ↓new Foo(this.A, this.B, this.C, this.D);
    }
}";

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void AllocatingReferenceTypeFromMutablePropertyNoFix2()
        {
            var before = @"
namespace N
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

        public Foo Bar => ↓new Foo(A, B, C, D);
    }
}";

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void AllocatingReferenceTypeFromMutableFieldNoFix()
        {
            var before = @"
namespace N
{
    public class C
    {
        public readonly int a;
        public readonly int b;
        public readonly int c;
        public int d;

        public C(int a, int b, int c, int d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        public C P => ↓new C(this.a, this.b, this.c, this.d);
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        public readonly int a;
        public readonly int b;
        public readonly int c;
        public int d;

        public C(int a, int b, int c, int d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
            this.P = new C(this.a, this.b, this.c, this.d);
        }

        public C P { get; }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void AllocatingReferenceTypeFromMutableFieldUnderscoreNoFix()
        {
            var before = @"
namespace N
{
    public class C
    {
        public readonly int _a;
        public readonly int _b;
        public readonly int _c;
        public int _d;

        public C(int a, int b, int c, int d)
        {
            _a = a;
            _b = b;
            _c = c;
            _d = d;
        }

        public C P => ↓new C(_a, _b, _c, _d);
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        public readonly int _a;
        public readonly int _b;
        public readonly int _c;
        public int _d;

        public C(int a, int b, int c, int d)
        {
            _a = a;
            _b = b;
            _c = c;
            _d = d;
            P = new C(_a, _b, _c, _d);
        }

        public C P { get; }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void AllocatingReferenceTypeFromMutableMembersObjectInitializerNoFix()
        {
            var before = @"
namespace N
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

        public Foo Bar => ↓new Foo(this.A, this.B, this.C, 0)
            {
                D = this.D
            };
    }
}";

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void AllocatingReferenceTypeFromSecondLevelNoFix1()
        {
            var before = @"
namespace N
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
    
        public Foo Bar2 => ↓new Foo(this.A, this.B, this.C, this.Bar1.D);
    }
}";

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void AllocatingReferenceTypeFromSecondLevelNoFix2()
        {
            var before = @"
namespace N
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
    
        public Foo Bar2 => ↓new Foo(this.A, this.B, this.C, this.Bar1.D);
    }
}";

            var after = @"
namespace N
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }
    }
}
