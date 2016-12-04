namespace Gu.Analyzers.Test.GU0021CalculatedPropertyAllocatesTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0021CalculatedPropertyAllocates>
    {
        [Test]
        public async Task ArrowAdd()
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

        public int Sum => this.A + this.B + this.C + this.D;
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ArrowStruct()
        {
            var testCode = @"
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

        public Foo Sum => new Foo(this.A, this.B, this.C, this.D);
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}