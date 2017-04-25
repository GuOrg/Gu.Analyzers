namespace Gu.Analyzers.Test.GU0007PreferInjectingTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<GU0007PreferInjecting>
    {
        [Test]
        public async Task WhenInjecting()
        {
            var fooCode = @"
    public class Foo
    {
        private readonly Bar bar;

        public Foo(Bar bar)
        {
            this.bar = bar;
        }
    }";

            var barCode = @"
    public class Bar
    {
    }";
            await this.VerifyHappyPathAsync(fooCode, barCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenNotInjectingChained()
        {
            var fooCode = @"
    public class Foo
    {
        private readonly Bar bar;

        public Foo(Bar bar)
        {
            this.bar = bar;
        }
    }";
            var barCode = @"
    public class Bar
    {
        private readonly int value;

        public Bar(int value)
        {
            this.value = value;
        }
    }";

            var mehCode = @"
    public class Meh : Foo
    {
        public Meh()
           : base(new Bar(1))
        {
        }
    }";
            await this.VerifyHappyPathAsync(new[] { fooCode, barCode, mehCode })
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenStatic()
        {
            var fooCode = @"
    public static class Foo
    {
        private static Bar bar;

        static Foo()
        {
            bar = new Bar();
        }
    }";

            var barCode = @"
    public class Bar
    {
    }";
            await this.VerifyHappyPathAsync(fooCode, barCode)
                      .ConfigureAwait(false);
        }
    }
}