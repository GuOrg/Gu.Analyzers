namespace Gu.Analyzers.Test.GU0010DoNotAssignSameValueTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0010DoNotAssignSameValue>
    {
        [Test]
        public async Task ConstructorSettingProperties()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ConstructorSettingFields()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task Increment()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ObjectInitializer()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ObjectInitializerStruct()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [TestCase("foo.A = A;", "foo.A = this.A;")]
        [TestCase("foo.A = A;", "foo.A = A;")]
        public async Task SetSameMemberOnOtherInstance(string before, string after)
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
            testCode = testCode.AssertReplace(before, after);
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task SetSameMemberOnOtherInstance2()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task SetSameMemberOnOtherInstanceRecursive()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}