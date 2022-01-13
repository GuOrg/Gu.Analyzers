namespace Gu.Analyzers.Test.GU0010DoNotAssignSameValueTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class Valid
{
    private static readonly SimpleAssignmentAnalyzer Analyzer = new();

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
    public class C
    {
        private readonly int value1;
        private readonly int value2;

        public C(int value1, int value2)
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
    public class C
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
    public class C
    {
        public int A { get; private set; }

        public C Clone()
        {
            return new C { A = A };
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

    [TestCase("c.A = this.A;")]
    [TestCase("c.A = A;")]
    public static void SetSameMemberOnOtherInstance(string after)
    {
        var code = @"
namespace N
{
    public class C
    {
        public int A { get; private set; }

        public void M()
        {
            var c = new C();
            c.A = A;
        }
    }
}".AssertReplace("c.A = A;", after);

        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void SetSameMemberOnOtherInstance2()
    {
        var code = @"
namespace N
{
    public class C
    {
        public int A { get; private set; }

        public void M()
        {
            var c1 = new C();
            var c2 = new C();
            c1.A = c2.A;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void SetSameMemberOnOtherInstanceRecursive()
    {
        var code = @"
#nullable disable
namespace N
{
    public class C
    {
        public C Next { get; private set; }

        public void M()
        {
            var c1 = new C();
            var c2 = new C();
            c1.Next.Next = c2.Next.Next;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }
}
