namespace Gu.Analyzers.Test.GU0030UseUsingTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0030UseUsing>
    {
        [Test]
        public async Task UsingFileStream()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static long Bar()
        {
            using (var stream = File.OpenRead(""""))
            {
                return stream.Length;
            }
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DontUseUsingWhenAssigningAField()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        private static readonly Stream Stream = File.OpenRead("""");

        public static long Bar()
        {
            var stream = Stream;
            return stream.Length;
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DontUseUsingWhenAssigningACallThatReturnsAField()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        private static readonly Stream Stream = File.OpenRead("""");

        public static long Bar()
        {
            var stream = GetStream();
            return stream.Length;
        }

        public static Stream GetStream()
        {
            return Stream;
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task UsingNewDisposable()
        {
            var disposableCode = @"
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }";

            var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static long Bar()
        {
            using (var meh = new Disposable())
            {
                return 1;
            }
        }
    }";
            await this.VerifyHappyPathAsync(testCode, disposableCode)
                      .ConfigureAwait(false);
        }
    }
}