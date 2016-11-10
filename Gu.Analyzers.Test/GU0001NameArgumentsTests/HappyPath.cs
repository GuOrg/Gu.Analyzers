namespace Gu.Analyzers.Test.GU0001NameArgumentsTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0001NameArguments>
    {
        [TestCase("new Foo(a, b)")]
        [TestCase("new Foo(a: a, b: b)")]
        public async Task ConstructorCallWithTwoArguments(string call)
        {
            var testCode = @"
    public class Foo
    {
        public Foo(int a, int b)
        {
            this.A = a;
            this.B = b;
        }

        public int A { get; }

        public int B { get; }

        private Foo Create(int a, int b, int c, int d)
        {
            return new Foo(a, b);
        }
    }";
            testCode = testCode.AssertReplace("new Foo(a, b)", call);
            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task ConstructorCallWithNamedArguments()
        {
            var testCode = @"
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

        private Foo Create(int a, int b, int c, int d)
        {
            return new Foo(a: a, b: b, c: c, d: d);
        }
    }";

            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }
    }
}