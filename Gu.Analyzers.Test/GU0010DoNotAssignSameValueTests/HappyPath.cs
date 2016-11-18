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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task Increment()
        {
            var testCode = @"
    public class Foo
    {
        public int A { get; private set; }

        private void Increment()
        {
            A = A + 1;
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}