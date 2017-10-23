namespace Gu.Analyzers.Test.GU0010DoNotAssignSameValueTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly GU0010DoNotAssignSameValue Analyzer = new GU0010DoNotAssignSameValue();

        [Test]
        public void ConstructorSettingProperties()
        {
            var testCode = @"
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ConstructorSettingFields()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void Increment()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ObjectInitializer()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ObjectInitializerStruct()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase("foo.A = this.A;")]
        [TestCase("foo.A = A;")]
        public void SetSameMemberOnOtherInstance(string after)
        {
            var testCode = @"
namespace RoslynSandbox
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
}";
            testCode = testCode.AssertReplace("foo.A = A;", after);
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void SetSameMemberOnOtherInstance2()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void SetSameMemberOnOtherInstanceRecursive()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}