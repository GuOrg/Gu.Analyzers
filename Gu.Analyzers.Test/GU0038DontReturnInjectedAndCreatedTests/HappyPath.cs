namespace Gu.Analyzers.Test.GU0038DontReturnInjectedAndCreatedTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<GU0038DontReturnInjectedAndCreated>
    {
        [Test]
        public async Task ReturningCreatedStatementBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        public Stream Get(string text)
        {
            if (text == null)
            {
               return File.OpenRead(string.Empty);
            }

            return File.OpenRead(text);
        }
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ReturningCreatedExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        public Stream Get(string text) => File.OpenRead(string.Empty);
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ReturningField()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public sealed class Foo
    {
        private readonly Stream stream = File.OpenRead(string.Empty);

        public Stream Get(string text)
        {
            if (text == null)
            {
                return this.stream;
            }

            return this.stream;
        }
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}