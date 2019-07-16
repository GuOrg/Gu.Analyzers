namespace Gu.Analyzers.Test.GU0010DoNotAssignSameValueTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class ValidCode
    {
        private static readonly SimpleAssignmentAnalyzer Analyzer = new SimpleAssignmentAnalyzer();

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
        public static void ConstructorSettingFields()
        {
            var code = @"
namespace N
{
    public class Foo
    {
        private readonly int value1;
        private readonly int value2;

        public Foo(int value1, int value2)
        {
            this.value1 = value1;
            this.value2 = value2;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void Increment()
        {
            var code = @"
namespace N
{
    public class Foo
    {
        public int A { get; private set; }

        private void Increment()
        {
            A = A + 1;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ObjectInitializer()
        {
            var code = @"
namespace N
{
    public class Foo
    {
        public int A { get; private set; }

        public Foo Clone()
        {
            return new Foo { A = A };
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ObjectInitializerStruct()
        {
            var code = @"
namespace N
{
    public struct Foo
    {
        public int A { get; private set; }

        public Foo Clone()
        {
            return new Foo { A = A };
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("foo.A = this.A;")]
        [TestCase("foo.A = A;")]
        public static void SetSameMemberOnOtherInstance(string after)
        {
            var code = @"
namespace N
{
    public class Foo
    {
        public int A { get; private set; }

        public void Meh()
        {
            var foo = new Foo();
            foo.A = A;
        }
    }
}".AssertReplace("foo.A = A;", after);

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void SetSameMemberOnOtherInstance2()
        {
            var code = @"
namespace N
{
    public class Foo
    {
        public int A { get; private set; }

        public void Meh()
        {
            var foo1 = new Foo();
            var foo2 = new Foo();
            foo1.A = foo2.A;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void SetSameMemberOnOtherInstanceRecursive()
        {
            var code = @"
namespace N
{
    public class Foo
    {
        public Foo Next { get; private set; }

        public void Meh()
        {
            var foo1 = new Foo();
            var foo2 = new Foo();
            foo1.Next.Next = foo2.Next.Next;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
