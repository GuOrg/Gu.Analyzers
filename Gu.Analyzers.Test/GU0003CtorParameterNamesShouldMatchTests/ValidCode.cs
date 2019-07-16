namespace Gu.Analyzers.Test.GU0003CtorParameterNamesShouldMatchTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ConstructorAnalyzer();

        [Test]
        public static void ConstructorSettingProperties()
        {
            var code = @"
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
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ConstructorSettingPropertiesStruct()
        {
            var code = @"
namespace N
{
    public struct Foo
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ChainedConstructorSettingProperties()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void BaseConstructorCall()
        {
            var fooCode = @"
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
    }
}";

            var barCode = @"
namespace N
{
    public class Bar : Foo
    {
        public Bar(int a, int b, int c, int d)
            : base(a, b, c, d)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, fooCode, barCode);
        }

        [Test]
        public static void ConstructorSettingField()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ConstructorSettingFieldPrefixedByUnderscore()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoresWhenSettingTwoProperties()
        {
            var code = @"
namespace N
{
    public class Foo
    {
        public Foo(int a)
        {
            this.A = a;
            this.B = a;
        }

        public int A { get; }

        public int B { get; }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoresWhenBaseIsParams()
        {
            var fooCode = @"
namespace N
{
    public class Bar : Foo
    {
        public Bar(int a, int b, int c, int d)
            : base(a, b, c, d)
        {
        }
    }
}";
            var barCode = @"
namespace N
{
    public class Foo
    {
        public Foo(params int[] values)
        {
            this.Values = values;
        }

        public int[] Values { get; }
    }
}";
            RoslynAssert.Valid(Analyzer, fooCode, barCode);
        }

        [Test]
        public static void IgnoresWhenBaseIsParams2()
        {
            var fooCode = @"
namespace N
{
    public class Bar : Foo
    {
        public Bar(int a, int b, int c, int d)
            : base(a, b, c, d)
        {
        }
    }
}";
            var barCode = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, fooCode, barCode);
        }

        [Test]
        public static void IgnoresIdCaps()
        {
            var code = @"
namespace N
{
    public class Foo
    {
        public Foo(int id)
        {
            this.ID = id;
        }

        public int ID { get; }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoresTupleCreate()
        {
            var code = @"
namespace N
{
    using System;

    public class Foo
    {
        public Foo()
        {
            var tuple = Tuple.Create(
                1,
                2,
                3,
                4);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoresNumbered()
        {
            var code = @"
namespace N
{
    public class Foo : Bar
    {
        public Foo(int x, int y, int z)
            : base(x, y, z)
        {
        }
    }

    public class Bar
    {
        public Bar(int value1, int value2, int value3)
        {
            this.Values = new int[] { value1, value2, value3 };
        }

        public int[] Values { get; }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoredWhenAssigningWeakReferenceTarget()
        {
            var code = @"
namespace N
{
    using System;
    using System.Text;

    public class Foo
    {
        private readonly WeakReference wr = new WeakReference(null);

        public Foo(StringBuilder builder)
        {
            this.wr.Target = builder;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenUsingParameterAsTwoArguments()
        {
            var code = @"
namespace N
{
    public sealed class Foo
    {
        private readonly int a;
        private readonly int b;

        public Foo(int x)
            : this(x, x)
        {
        }

        public Foo(int a, int b)
        {
            this.a = a;
            this.b = b;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ThrowExpressionNameofOther()
        {
            var code = @"
namespace N
{
    using System;

    public class Foo
    {
        private readonly string sq;

        public Foo(string s1, string s2)
        {
            this.sq = s1 ?? throw new ArgumentNullException(nameof(s2));
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void NullChecksAndThrowingForWrong()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        private readonly string s1;
        private readonly string s2;

        public C(string s1, string s2)
        {
            if (s2 == null)
            {
                throw new System.ArgumentNullException(nameof(s1));
            }

            this.s1 = s1 ?? throw new System.ArgumentNullException(nameof(s2));
            this.s2 = s2 + ""abc"";
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
