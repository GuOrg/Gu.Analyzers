namespace Gu.Analyzers.Test.GU0030UseUsingTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<GU0030UseUsing>
    {
        private static readonly string DisposableCode = @"
using System;

public class Disposable : IDisposable
{
    public Disposable(string meh)
        : this()
    {
    }

    public Disposable()
    {
    }

    public void Dispose()
    {
    }
}";

        [Test]
        public async Task WhenDisposingVariable()
        {
            var testCode = @"
public class Foo
{
    public void Meh()
    {
        var item = new Disposable();
        item.Dispose();
    }
}";

            await this.VerifyHappyPathAsync(DisposableCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task UsingFileStream()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static long Bar()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                return stream.Length;
            }
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

        [Test]
        public async Task HandlesRecursion()
        {
            var testCode = @"
    using System;

    public static class Foo
    {
        public static void Bar()
        {
            var disposable = Forever();
        }

        private static IDisposable Forever()
        {
            return Forever();
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task Awaiting()
        {
            var testCode = @"
using System.IO;
using System.Threading.Tasks;
  
internal static class Foo
{
    internal static async Task Bar()
    {
        using (var stream = await ReadAsync(string.Empty))
        {
        }
    }

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
        public async Task FactoryMethod()
        {
            var testCode = @"
using System;
using System.IO;

public class Disposal : IDisposable
{
	private Stream stream;

	public Disposal() :
		this(File.OpenRead(string.Empty))
	{
	}

	private Disposal(Stream stream)
	{
		this.stream = stream;
	}

	public static Disposal CreateNew()
	{
		Stream stream = File.OpenRead(string.Empty);
		return new Disposal(stream);
	}

	public void Dispose()
	{
		if (stream != null)
		{
			stream.Dispose();
			stream = null;
		}
	}
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [TestCase("disposables.First();")]
        [TestCase("disposables.First(x => x != null);")]
        [TestCase("disposables.Where(x => x != null);")]
        [TestCase("disposables.Single();")]
        [TestCase("Enumerable.Empty<IDisposable>();")]
        public async Task IgnoreLinq(string linq)
        {
            var testCode = @"
using System;
using System.Linq;

public sealed class Foo
{
    public Foo(IDisposable[] disposables)
    {
        var first = disposables.First();
    }
}";
            testCode = testCode.AssertReplace("disposables.First();", linq);
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedDbConnectionCreateCommand()
        {
            var testCode = @"
using System.Data.Common;

public class Foo
{
	public static void Bar(DbConnection conn)
	{
		using(var command = conn.CreateCommand())
		{
		}
	}
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedMemberDbConnectionCreateCommand()
        {
            var testCode = @"
using System.Data.Common;

public class Foo
{
    private readonly DbConnection connection;

	public Foo(DbConnection connection)
	{
		this.connection = connection;
	}

	public void Bar()
	{
		using(var command = this.connection.CreateCommand())
		{
		}
	}
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}