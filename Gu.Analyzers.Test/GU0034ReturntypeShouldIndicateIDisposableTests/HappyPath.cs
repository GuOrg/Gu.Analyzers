namespace Gu.Analyzers.Test.GU0034ReturntypeShouldIndicateIDisposableTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0034ReturntypeShouldIndicateIDisposable>
    {
        [Test]
        public async Task VoidMethodReturn()
        {
            var testCode = @"
    public class Foo
    {
        public void Bar()
        {
            Meh();
        }

        private static void Meh()
        {
            return;
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

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

        private static object Meh()
        {
            return new object();
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task MethodReturningFuncObject()
        {
            var testCode = @"
using System;

public class Foo
{
    public void Bar()
    {
        Meh();
    }

    private static Func<object> Meh()
    {
        return () => new object();
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task MethodReturningObjectExpressionBody()
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
        public async Task PropertyReturningObject()
        {
            var testCode = @"
    public class Foo
    {
        public void Bar()
        {
            var meh = Meh;
        }

        public object Meh
        {
            get
            {
                return new object();
            }
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task GenericMethod()
        {
            var testCode = @"
    public class Foo
    {
        public void Bar()
        {
            Id(1);
        }

        private static T Id<T>(T meh)
        {
            return meh;
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task PropertyReturningObjectExpressionBody()
        {
            var testCode = @"
    public class Foo
    {
        public void Bar()
        {
            var meh = Meh;
        }

        public object Meh => new object();
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ReturningFileOpenReadAsStream()
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
        public async Task ReturnDisposableFieldAsObject()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo
{
    private readonly Stream stream = File.OpenRead("""");

    public object Meh()
    {
        return stream;
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}