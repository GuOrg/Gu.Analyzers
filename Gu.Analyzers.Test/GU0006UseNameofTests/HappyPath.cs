namespace Gu.Analyzers.Test.GU0006UseNameofTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0006UseNameof>
    {
        [Test]
        public async Task WhenThrowingArgumentException()
        {
            var testCode = @"
    using System;

    public class Foo
    {
        public void Meh(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}