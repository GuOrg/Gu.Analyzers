// ReSharper disable InconsistentNaming
namespace Gu.Analyzers.Test.GU0034ReturntypeShouldIndicateIDisposableTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<GU0034ReturntypeShouldIndicateIDisposable>
    {
        [Test]
        public async Task RealisticExtensionMethodClass()
        {
            var testCode = @"
    using System;
    using System.Collections.Generic;

    internal static class EnumerableExt
    {
        internal static bool TryGetAtIndex<TCollection, TItem>(this TCollection source, int index, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            result = default(TItem);
            if (source == null)
            {
                return false;
            }

            if (source.Count <= index)
            {
                return false;
            }

            result = source[index];
            return true;
        }

        internal static bool TryGetSingle<TCollection, TItem>(this TCollection source, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            if (source.Count == 1)
            {
                result = source[0];
                return true;
            }

            result = default(TItem);
            return false;
        }

        internal static bool TryGetSingle<TCollection, TItem>(this TCollection source, Func<TItem, bool> selector, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(TItem);
            return false;
        }

        internal static bool TryGetFirst<TCollection, TItem>(this TCollection source, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            if (source.Count == 0)
            {
                result = default(TItem);
                return false;
            }

            result = source[0];
            return true;
        }

        internal static bool TryGetFirst<TCollection, TItem>(this TCollection source, Func<TItem, bool> selector, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            foreach (var item in source)
            {
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(TItem);
            return false;
        }

        internal static bool TryGetLast<TCollection, TItem>(this TCollection source, out TItem result)
            where TCollection : IReadOnlyList<TItem>
        {
            if (source.Count == 0)
            {
                result = default(TItem);
                return false;
            }

            result = source[source.Count - 1];
            return true;
        }

        internal static bool TryGetLast<TCollection, TItem>(this TCollection source, Func<TItem, bool> selector, out TItem result)
             where TCollection : IReadOnlyList<TItem>
        {
            for (var i = source.Count - 1; i >= 0; i--)
            {
                var item = source[i];
                if (selector(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(TItem);
            return false;
        }
    }";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

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
        public async Task IndexerReturningObject()
        {
            var testCode = @"
public class Foo
{
    public void Bar()
    {
        var meh = this[0];
    }

    public object this[int index]
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
        public async Task IEnumerableOfIntSimple()
        {
            var testCode = @"
using System.Collections;
using System.Collections.Generic;

public class Foo : IEnumerable
{
    private readonly List<int> ints = new List<int>();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.ints.GetEnumerator();
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

        [Test]
        public async Task ReturningAsyncTaskOfStream()
        {
            var testCode = @"
using System;
using System.IO;
using System.Threading.Tasks;

internal static class Foo
{
    internal static async Task<Stream> ReadAsync(string file)
    {
	    var stream = new MemoryStream();
	    using (var fileStream = File.OpenRead(file))
	    {
		    await fileStream.CopyToAsync(stream)
						    .ConfigureAwait(false);
	    }

	    stream.Position = 0;
	    return stream;
    }
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task Lambda()
        {
            var testCode = @"
using System;
using System.IO;

internal static class Foo
{
    internal static void Bar()
    {
        Func<IDisposable> f = () =>
        {
	        var file = System.IO.File.OpenRead(null);
	        return file;
        };
    }
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}