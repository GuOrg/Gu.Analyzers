namespace Gu.Analyzers.Test.GU0003CtorParameterNamesShouldMatchTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class Valid
{
    private static readonly ConstructorAnalyzer Analyzer = new();

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
        var c1 = @"
namespace N
{
    public class C1
    {
        public C1(int a, int b, int c, int d)
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

        var c2 = @"
namespace N
{
    public class C2 : C1
    {
        public C2(int a, int b, int c, int d)
            : base(a, b, c, d)
        {
        }
    }
}";
        RoslynAssert.Valid(Analyzer, c1, c2);
    }

    [Test]
    public static void ConstructorSettingField()
    {
        var code = @"
namespace N
{
    public class C
    {
        private readonly int a;
        private readonly int b;
        private readonly int c;
        private readonly int d;

        public C(int a, int b, int c, int d)
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
    public class C
    {
        private readonly int _a;
        private readonly int _b;
        private readonly int _c;
        private readonly int _d;

        public C(int a, int b, int c, int d)
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
    public class C
    {
        public C(int a)
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
        var c1 = @"
namespace N
{
    public class C1
    {
        public C1(params int[] values)
        {
            this.Values = values;
        }

        public int[] Values { get; }
    }
}";

        var c2 = @"
namespace N
{
    public class C2 : C1
    {
        public C2(int a, int b, int c, int d)
            : base(a, b, c, d)
        {
        }
    }
}";

        RoslynAssert.Valid(Analyzer, c1, c2);
    }

    [Test]
    public static void IgnoresWhenBaseIsParams2()
    {
        var c1 = @"
namespace N
{
    public class C1
    {
        public C1(int a, params int[] values)
        {
            this.A = a;
            this.Values = values;
        }

        public int A { get; }

        public int[] Values { get; }
    }
}";
        var c2 = @"
namespace N
{
    public class C2 : C1
    {
        public C2(int a, int b, int c, int d)
            : base(a, b, c, d)
        {
        }
    }
}";

        RoslynAssert.Valid(Analyzer, c1, c2);
    }

    [Test]
    public static void IgnoresIdCaps()
    {
        var code = @"
namespace N
{
    public class C
    {
        public C(int id)
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

    public class C
    {
        public C()
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
    public class C1 : C2
    {
        public C1(int x, int y, int z)
            : base(x, y, z)
        {
        }
    }

    public class C2
    {
        public C2(int value1, int value2, int value3)
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

    public class C
    {
        private readonly WeakReference wr = new WeakReference(null);

        public C(StringBuilder builder)
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
    public sealed class C
    {
        private readonly int a;
        private readonly int b;

        public C(int x)
            : this(x, x)
        {
        }

        public C(int a, int b)
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

    public class C
    {
        private readonly string sq;

        public C(string s1, string s2)
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
            if (s2 is null)
            {
                throw new ArgumentNullException(nameof(s1));
            }

            this.s1 = s1 ?? throw new ArgumentNullException(nameof(s2));
            this.s2 = s2 + ""abc"";
        }
    }
}";

        RoslynAssert.Valid(Analyzer, code);
    }
}
