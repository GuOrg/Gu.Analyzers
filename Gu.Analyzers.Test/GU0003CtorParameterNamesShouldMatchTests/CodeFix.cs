namespace Gu.Analyzers.Test.GU0003CtorParameterNamesShouldMatchTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class CodeFix
    {
        private static readonly ConstructorAnalyzer Analyzer = new ConstructorAnalyzer();
        private static readonly RenameConstructorParameterFix Fix = new RenameConstructorParameterFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0003CtorParameterNamesShouldMatch.Descriptor);

        [Test]
        public static void Message()
        {
            var before = @"
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

            var after = @"
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
            var expectedDiagnostic = ExpectedDiagnostic.WithMessage("Name the parameter to match the assigned member.");
            RoslynAssert.CodeFix(Analyzer, Fix, expectedDiagnostic, before, after, fixTitle: "Rename to 'a'");
        }

        [Test]
        public static void ConstructorSettingProperties()
        {
            var before = @"
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

            var after = @"
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void ChainedConstructor()
        {
            var before = @"
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

            var after = @"
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void BaseConstructorCall()
        {
            var before = @"
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

            var after = @"
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { before, barCode }, after);
        }

        [Test]
        public static void ConstructorSettingFields()
        {
            var before = @"
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

            var after = @"
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void ConstructorSettingFieldsPrefixedWithUnderscore()
        {
            var before = @"
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

            var after = @"
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenBaseIsParams()
        {
            var before = @"
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

            var after = @"
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { before, barCode }, after);
        }

        [Test]
        public static void WhenSettingPropertyAndChained()
        {
            var before = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int ↓a1)
            :this(a1, 1)
        {
            this.B = a1;
        }

        public Foo(int a, int b)
        {
            this.B = 0;
        }

        public int B { get; }
    }
}";

            var after = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int b)
            :this(b, 1)
        {
            this.B = b;
        }

        public Foo(int a, int b)
        {
            this.B = 0;
        }

        public int B { get; }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenAssignAndBaseCall()
        {
            var before = @"
namespace RoslynSandbox
{
    public class Bar : Foo
    {
        public Bar(int ↓x)
            : base(x)
        {
            this.A = x;
        }

        public int A { get; }
    }
}";
            var barCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int b)
        {
        }
    }
}";

            var after = @"
namespace RoslynSandbox
{
    public class Bar : Foo
    {
        public Bar(int a)
            : base(a)
        {
            this.A = a;
        }

        public int A { get; }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { before, barCode }, after);
        }
    }
}
