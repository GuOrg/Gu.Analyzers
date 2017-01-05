namespace Gu.Analyzers.Test.GU0020SortPropertiesTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    [Explicit("Fix later")]
    internal class CodeFixAll : CodeFixVerifier<GU0020SortProperties, SortPropertiesCodeFixProvider>
    {
        [Test]
        public async Task WhenMutableBeforeGetOnlyFirst()
        {
            var testCode = @"
    public class Foo
    {
        public int A { get; set; }

        public int B { get; }

        public int C { get; }

        public int D { get; }
    }";

            var fixedCode = @"
    public class Foo
    {
        public int B { get; }

        public int C { get; }

        public int D { get; }

        public int A { get; set; }
    }";
            await this.VerifyCSharpFixAllFixAsync(testCode, fixedCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenAMess1()
        {
            var testCode = @"
    public class Foo
    {
        public int A { get; set; }

        public int B { get; private set; }

        public int C { get; }

        public int D => C;
    }";

            var fixedCode = @"
    public class Foo
    {
        public int C { get; }

        public int D => C;

        public int B { get; private set; }

        public int A { get; set; }
    }";
            await this.VerifyCSharpFixAllFixAsync(testCode, fixedCode)
                      .ConfigureAwait(false);
        }
    }
}