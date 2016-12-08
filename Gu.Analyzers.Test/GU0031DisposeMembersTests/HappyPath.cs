namespace Gu.Analyzers.Test.GU0031DisposeMembersTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0031DisposeMembers>
    {
        [Test]
        public async Task DisposingField()
        {
            var testCode = @"
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream = File.OpenRead("""");
        
        public void Dispose()
        {
            this.stream.Dispose();
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}