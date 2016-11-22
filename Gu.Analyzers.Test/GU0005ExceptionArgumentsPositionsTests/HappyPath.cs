namespace Gu.Analyzers.Test.GU0005ExceptionArgumentsPositionsTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0005ExceptionArgumentsPositions>
    {
        [Test]
        public async Task ArgumentExceptionWithMessageAndNameof()
        {
            var testCode = @"
    using System;

    public class Foo
    {
        public Foo(object o)
        {
            throw new ArgumentException(""message"", nameof(o));
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ArgumentNullExceptionWithMessageAndNameof()
        {
            var testCode = @"
    using System;

    public class Foo
    {
        public Foo(object o)
        {
            throw new ArgumentNullException(nameof(o), ""message"");
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ArgumentOutOfRangeExceptionWithMessageAndNameof()
        {
            var testCode = @"
    using System;

    public class Foo
    {
        public Foo(object o)
        {
            throw new ArgumentOutOfRangeException(nameof(o), ""message"");
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}