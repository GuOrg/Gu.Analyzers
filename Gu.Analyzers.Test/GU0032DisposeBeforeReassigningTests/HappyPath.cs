namespace Gu.Analyzers.Test.GU0032DisposeBeforeReassigningTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0032DisposeBeforeReassigning>
    {
        [Test]
        public async Task DisposingVariable()
        {
            var testCode = @"
    using System;
    using System.IO;

    public class Foo
    {
        public void Meh()
        {
            var stream = File.OpenRead(string.Empty);
            stream.Dispose();
            stream = File.OpenRead(string.Empty);
        }
    }";

            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task DisposingFieldInCtor()
        {
            var testCode = @"
    using System;
    using System.IO;

    public class Foo
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        public Foo()
        {
            stream.Dispose();
            stream = File.OpenRead(string.Empty);
        }
    }";
            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task DisposingFieldInMethod()
        {
            var testCode = @"
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public void Meh()
        {
            this.stream.Dispose();
            this.stream = File.OpenRead(string.Empty);
        }
    }";
            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task ConditionallyDisposingFieldInMethod()
        {
            var testCode = @"
    using System;
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public void Meh()
        {
            this.stream?.Dispose();
            this.stream = File.OpenRead(string.Empty);
        }
    }";
            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task ConditionallyDisposingUnderscoreFieldInMethod()
        {
            var testCode = @"
    using System;
    using System.IO;

    public class Foo
    {
        private Stream _stream;

        public void Meh()
        {
            _stream?.Dispose();
            _stream = File.OpenRead(string.Empty);
        }
    }";
            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task DisposingUnderscoreFieldInMethod()
        {
            var testCode = @"
    using System;
    using System.IO;

    public class Foo
    {
        private Stream _stream;

        public void Meh()
        {
            _stream.Dispose();
            _stream = File.OpenRead(string.Empty);
        }
    }";
            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }
    }
}