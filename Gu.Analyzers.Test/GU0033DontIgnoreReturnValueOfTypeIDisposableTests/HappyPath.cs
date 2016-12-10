namespace Gu.Analyzers.Test.GU0033DontIgnoreReturnValueOfTypeIDisposableTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0033DontIgnoreReturnValueOfTypeIDisposable>
    {
        [Test]
        public async Task MethodReturningObject()
        {
            var testCode = @"
    public class Foo
    {
        public void Bar()
        {
            Meh();
        }

        private static object Meh() => new object();
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task MethodWithArgReturningObject()
        {
            var testCode = @"
    public class Foo
    {
        public void Bar()
        {
            Meh(""Meh"");
        }

        private static object Meh(string arg) => new object();
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task MethodWithObjArgReturningObject()
        {
            var testCode = @"
    public class Foo
    {
        public void Bar()
        {
            Id(new Foo());
        }

        private static object Id(object arg) => arg;
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task Returning()
        {
            var testCode = @"
    using System.IO;

    public class Foo
    {
        public Stream Bar()
        {
            return File.OpenRead("""");
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task AllowPassingIntoStreamReader()
        {
            var testCode = @"
    using System.IO;

    public class Foo
    {
        public StreamReader Bar()
        {
            return new StreamReader(File.OpenRead(""""));
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}