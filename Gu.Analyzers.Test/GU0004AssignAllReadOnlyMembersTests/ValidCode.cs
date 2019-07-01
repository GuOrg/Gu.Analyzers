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
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ConstructorSettingPropertiesStruct()
        {
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void StaticConstructorSettingProperties()
        {
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ConstructorSettingAllFields()
        {
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ConstructorSettingFieldRef()
        {
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ConstructorSettingFieldOut()
        {
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ChainedConstructorSettingAllFields()
        {
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void WhenNoUninitializedFields()
        {
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void StaticConstructorSettingFields()
        {
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void StaticConstructorSettingQualifiedFields()
        {
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void StaticConstructorSettingUninitializedField()
        {
            var testCode = @"
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

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ConstructorSettingReadonlyFieldIgnoringMutable()
        {
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ConstructorSettingPropertiesIgnoringMutable()
        {
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ConstructorSettingReadonlyFieldIgnoringInitialized()
        {
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ConstructorSettingPropertiesIgnoringInitialized()
        {
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void IgnoreAbstract()
        {
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void IgnoreIndexer()
        {
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void IgnoreCalculatedStatementBody()
        {
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void IgnoreCalculatedExpressionBody()
        {
            var testCode = @"
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
            RoslynAssert.Valid(Analyzer, testCode);
        }
    }
}
