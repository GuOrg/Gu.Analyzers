namespace Gu.Analyzers.Test.GU0004AssignAllReadOnlyMembersTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly ConstructorAnalyzer Analyzer = new ConstructorAnalyzer();

        [Test]
        public void ConstructorSettingProperties()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ConstructorSettingPropertiesStruct()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void StaticConstructorSettingProperties()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ConstructorSettingAllFields()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ConstructorSettingFieldRef()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ConstructorSettingFieldOut()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ChainedConstructorSettingAllFields()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenNoUninitializedFields()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void StaticConstructorSettingFields()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void StaticConstructorSettingQualifiedFields()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void StaticConstructorSettingUninitializedField()
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

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ConstructorSettingReadonlyFieldIgnoringMutable()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ConstructorSettingPropertiesIgnoringMutable()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ConstructorSettingReadonlyFieldIgnoringInitialized()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ConstructorSettingPropertiesIgnoringInitialized()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoreAbstract()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoreIndexer()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoreCalculatedStatementBody()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoreCalculatedExpressionBody()
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}