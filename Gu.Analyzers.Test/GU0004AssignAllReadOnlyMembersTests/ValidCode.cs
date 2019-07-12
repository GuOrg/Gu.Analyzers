namespace Gu.Analyzers.Test.GU0004AssignAllReadOnlyMembersTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class ValidCode
    {
        private static readonly ConstructorAnalyzer Analyzer = new ConstructorAnalyzer();

        [Test]
        public static void ConstructorSettingProperties()
        {
            var code = @"
namespace RoslynSandbox
{
    public class Foo
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
        public static void ConstructorSettingPropertiesStruct()
        {
            var code = @"
namespace RoslynSandbox
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
namespace RoslynSandbox
{
    public class Foo
    {
        static Foo()
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
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int a;
        private readonly int b;

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
        public static void ConstructorSettingFieldRef()
        {
            var code = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int a;

        public Foo()
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
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int a;

        public Foo()
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
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int a;
        private readonly int b;
        
        public Foo()
            : this(1, 2)
        {
        }
     
        private Foo(int a, int b)
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
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int a = 1;
        
        public Foo()
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
namespace RoslynSandbox
{
    public class Foo
    {
        public static readonly int A;

        public static readonly int B;

        static Foo()
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
namespace RoslynSandbox
{
    public class Foo
    {
        public static readonly int A;

        public static readonly int B;

        static Foo()
        {
            Foo.A = 1;
            Foo.B = 2;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void StaticConstructorSettingUninitializedField()
        {
            var code = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public static readonly int A;

        public static readonly int B = 2;

        static Foo()
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
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int a;
        private bool disposed;

        public Foo(int a)
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
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a)
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
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int a;
        private readonly int defaultValue = 5;

        public Foo(int a)
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
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a)
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
namespace RoslynSandbox
{
    public abstract class Foo
    {
        protected Foo(int value)
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
namespace RoslynSandbox
{
    public abstract class Foo
    {
        protected Foo(int value)
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
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int value;

        public Foo(int value)
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
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int value;

        public Foo(int value)
        {
            this.value = value;
        }

        public int Value => this.value;
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
