namespace Gu.Analyzers.Test.GU0030UseUsingTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class HappyPath
    {
        [Test]
        public async Task WhenDisposableIsReturnedMethodSimple()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static Stream Bar()
        {
            return File.OpenRead(string.Empty);
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenDisposableIsReturnedMethodBody()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static Stream Bar()
        {
            var stream = File.OpenRead(string.Empty);
            return stream;
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenStreamIsReturnedInStreamReaderMethodBody()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static StreamReader Bar()
        {
            var stream = File.OpenRead(string.Empty);
            return new StreamReader(stream);
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenStreamIsReturnedInCompositeDisposableMethodBody()
        {
            var testCode = @"
using System.IO;
using System.Reactive.Disposables;

public static class Foo
{
    public static CompositeDisposable Bar()
    {
        var stream = File.OpenRead(string.Empty);
        return new CompositeDisposable { stream };
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenDisposableIsReturnedMethodExpressionBody()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static Stream Bar() => File.OpenRead(string.Empty);
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenDisposableIsReturnedPropertySimple()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static Stream Bar
        {
            get
            {
                return File.OpenRead(string.Empty);;
            }
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenDisposableIsReturnedPropertyBody()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static Stream Bar
        {
            get
            {
                var stream = File.OpenRead(string.Empty);
                return stream;
            }
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenDisposableIsReturnedPropertyExpressionBody()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static Stream Bar => File.OpenRead(string.Empty);
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}