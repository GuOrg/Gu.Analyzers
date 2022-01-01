namespace Gu.Analyzers.Test.GU0004AssignAllReadOnlyMembersTests;

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
    public class C
    {
        public C(int a, int b)
        {
            this.A = a;
            this.B = b;
        }

        public int A { get; }

        public int B { get; }
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
        public Foo(int a, int b)
        {
            this.A = a;
            this.B = b;
        }

        public int A { get; }

        public int B { get; }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void StaticConstructorSettingProperties()
    {
        var code = @"
namespace N
{
    public class C
    {
        static C()
        {
            A = 1;
            B = 2;
        }

        public static int A { get; }

        public static int B { get; }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ConstructorSettingAllFields()
    {
        var code = @"
namespace N
{
    public class C
    {
        private readonly int a;
        private readonly int b;

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
    public static void ConstructorSettingFieldRef()
    {
        var code = @"
namespace N
{
    public class C
    {
        private readonly int a;

        public C()
        {
            Meh(ref this.a);
        }

        private static void Meh(ref int i)
        {
            i = 1;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ConstructorSettingFieldOut()
    {
        var code = @"
namespace N
{
    public class C
    {
        private readonly int a;

        public C()
        {
            Meh(out this.a);
        }

        private static void Meh(out int i)
        {
            i = 1;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ChainedConstructorSettingAllFields()
    {
        var code = @"
namespace N
{
    public class C
    {
        private readonly int a;
        private readonly int b;
        
        public C()
            : this(1, 2)
        {
        }
     
        private C(int a, int b)
        {
            this.a = a;
            this.b = b;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void WhenNoUninitializedFields()
    {
        var code = @"
namespace N
{
    public class C
    {
        private readonly int a = 1;
        
        public C()
        {
        }
     
        public int A => a;
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void StaticConstructorSettingFields()
    {
        var code = @"
namespace N
{
    public class C
    {
        public static readonly int A;

        public static readonly int B;

        static C()
        {
            A = 1;
            B = 2;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void StaticConstructorSettingQualifiedFields()
    {
        var code = @"
namespace N
{
    public class C
    {
        public static readonly int A;

        public static readonly int B;

        static C()
        {
            C.A = 1;
            C.B = 2;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void StaticConstructorSettingUninitializedField()
    {
        var code = @"
namespace N
{
    public class C
    {
        public static readonly int A;

        public static readonly int B = 2;

        static C()
        {
            A = 1;
        }
    }
}";

        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ConstructorSettingReadonlyFieldIgnoringMutable()
    {
        var code = @"
#pragma warning disable CS0169
namespace N
{
    public class C
    {
        private readonly int a;

        private bool disposed;

        public C(int a)
        {
            this.a = a;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ConstructorSettingPropertiesIgnoringMutable()
    {
        var code = @"
namespace N
{
    public class C
    {
        public C(int a)
        {
            this.A = a;
        }

        public int A { get; }

        public int B { get; private set; }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ConstructorSettingReadonlyFieldIgnoringInitialized()
    {
        var code = @"
#pragma warning disable CS0414
namespace N
{
    public class C
    {
        private readonly int a;
        private readonly int defaultValue = 5;

        public C(int a)
        {
            this.a = a;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ConstructorSettingPropertiesIgnoringInitialized()
    {
        var code = @"
namespace N
{
    public class C
    {
        public C(int a)
        {
            this.A = a;
        }

        public int A { get; }

        public int B { get; } = 6;
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void IgnoreAbstract()
    {
        var code = @"
namespace N
{
    public abstract class C
    {
        protected C(int value)
        {
            this.Value = value;
        }

        public int Value { get; }

        public abstract int OtherValue { get; }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void IgnoreIndexer()
    {
        var code = @"
namespace N
{
    public abstract class C
    {
        protected C(int value)
        {
            this.Value = value;
        }

        public int Value { get; }

        public int this[int index] => index;
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void IgnoreCalculatedStatementBody()
    {
        var code = @"
namespace N
{
    public class C
    {
        private readonly int value;

        public C(int value)
        {
            this.value = value;
        }

        public int Value
        {
            get
            {
                return this.value;
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void IgnoreCalculatedExpressionBody()
    {
        var code = @"
namespace N
{
    public class C
    {
        private readonly int value;

        public C(int value)
        {
            this.value = value;
        }

        public int Value => this.value;
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }
}