namespace Gu.Analyzers.Test.GU0033DontIgnoreReturnValueOfTypeIDisposableTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class HappyPath
    {
        internal class Returned : NestedHappyPathVerifier<HappyPath>
        {
            [Test]
            public async Task Generic()
            {
                var factoryCode = @"
    public class Factory
    {
        public static T Create<T>() where T : new() => new T();
    }";

                var testCode = @"
    public class Foo
    {
        public void Bar()
        {
            Factory.Create<int>();
        }
    }";
                await this.VerifyHappyPathAsync(factoryCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task Operator()
            {
                var mehCode = @"
    public class Meh
    {
        public static Meh operator +(Meh left, Meh right) => new Meh();
    }";

                var testCode = @"
    public class Foo
    {
        public object Bar()
        {
            var meh1 = new Meh();
            var meh2 = new Meh();
            return meh1 + meh2;
        }
    }";
                await this.VerifyHappyPathAsync(mehCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task OperatorNestedCall()
            {
                var mehCode = @"
    public class Meh
    {
        public static Meh operator +(Meh left, Meh right) => new Meh();
    }";

                var testCode = @"
    public class Foo
    {
        public object Bar()
        {
            var meh1 = new Meh();
            var meh2 = new Meh();
            return Add(new Meh(), new Meh());
        }

        public object Add(Meh meh1, Meh meh2)
        {
            return meh1 + meh2;
        }
    }";
                await this.VerifyHappyPathAsync(mehCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task OperatorEquals()
            {
                var mehCode = @"
    public class Meh
    {
    }";

                var testCode = @"
    public class Foo
    {
        public bool Bar()
        {
            var meh1 = new Meh();
            var meh2 = new Meh();
            return meh1 == meh2;
        }
    }";
                await this.VerifyHappyPathAsync(mehCode, testCode)
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
            public async Task ReturningStatementBody()
            {
                var testCode = @"
namespace RoslynSandBox
{
    using System.IO;

    public class Foo
    {
        public Stream Bar()
        {
            return File.OpenRead(string.Empty);
        }
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task ReturningLocalStatementBody()
            {
                var testCode = @"
namespace RoslynSandBox
{
    using System.IO;

    public class Foo
    {
        public Stream Bar()
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
            public async Task ReturningExpressionBody()
            {
                var testCode = @"
namespace RoslynSandBox
{
    using System.IO;

    public class Foo
    {
        public Stream Bar() => File.OpenRead(string.Empty);
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task ReturningNewAssigningAndDispsing()
            {
                var fooCode = @"
using System;

public class Foo : IDisposable
{
    private readonly IDisposable disposable;

    public Foo(IDisposable disposable)
        :this()
    {
        this.disposable = disposable;
    }

    public Foo()
    {
    }

    public void Dispose()
    {
        this.disposable.Dispose();
    }
}";
                var testCode = @"
public class Meh
{
    public Foo Bar()
    {
        return new Foo(new Disposable());
    }
}";
                await this.VerifyHappyPathAsync(DisposableCode, fooCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task ReturningAssigningPrivateChained()
            {
                var fooCode = @"
using System;

public class Foo : IDisposable
{
    private readonly IDisposable disposable;

    public Foo(IDisposable disposable)
        :this()
    {
        this.disposable = disposable;
    }

    private Foo()
    {
    }

    public void Dispose()
    {
        this.disposable.Dispose();
    }
}";
                var testCode = @"
public class Meh
{
    public Foo Bar()
    {
        return new Foo(new Disposable());
    }
}";
                await this.VerifyHappyPathAsync(DisposableCode, fooCode, testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task StreamInStreamReader()
            {
                var testCode = @"
    using System.IO;

    public class Foo
    {
        public StreamReader Bar()
        {
            return new StreamReader(File.OpenRead(string.Empty));
        }
    }";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }
        }
    }
}