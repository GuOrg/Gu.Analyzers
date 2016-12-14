// ReSharper disable InconsistentNaming
namespace Gu.Analyzers.Test.GU0034ReturntypeShouldIndicateIDisposableTests
{
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
            return File.OpenRead(string.Empty);
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
    private readonly Stream stream = File.OpenRead(string.Empty);

    public object Meh()
    {
        return stream;
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IfTry()
        {
            var testCode = @"
public class Foo
{
    private void Bar()
    {
        int value;
        if(Try(out value))
        {
        }
    }

    private bool Try(out int value)
    {
        value = 1;
        return true;
    }
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IEnumerableOfInt()
        {
            var testCode = @"
using System.Collections;
using System.Collections.Generic;

public class Foo : IEnumerable<int>
{
    private readonly List<int> ints = new List<int>();

    public IEnumerator<int> GetEnumerator()
    {
        return this.ints.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IEnumerableOfIntExpressionBodies()
        {
            var testCode = @"
using System.Collections;
using System.Collections.Generic;

public class Foo : IEnumerable<int>
{
    private readonly List<int> ints = new List<int>();

    public IEnumerator<int> GetEnumerator() => this.ints.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}