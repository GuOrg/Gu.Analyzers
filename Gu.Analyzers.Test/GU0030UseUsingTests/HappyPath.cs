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
        public async Task DontUseUsingWhenAssigningAField()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        private static readonly Stream Stream = File.OpenRead(string.Empty);

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
        public async Task DontUseUsingWhenAssigningAFieldInAMethod()
        {
            var testCode = @"
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public void Bar()
        {
            this.stream = File.OpenRead(string.Empty);
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DontUseUsingWhenAssigningAFieldInAMethodLocalVariable()
        {
            var testCode = @"
    using System.IO;

    public class Foo
    {
        private Stream stream;

        public void Bar()
        {
            var newStream = File.OpenRead(string.Empty);
            this.stream = newStream;
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DontUseUsingWhenAddingLocalVariableToFieldList()
        {
            var testCode = @"
    using System.Collections.Generic;
    using System.IO;

    public class Foo
    {
        private readonly List<Stream> streams = new List<Stream>();

        public void Bar()
        {
            var stream = File.OpenRead(string.Empty);
            this.streams.Add(stream);
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
        public async Task AssignAssemblyLoadToLocal()
        {
            var testCode = @"
using System.Reflection;

public class Foo
{
public void Bar()
{
    var assembly = Assembly.Load(string.Empty);
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

        [Test]
        [Explicit("")]
        public async Task BuildCollectionThenAssignField()
        {
            var testCode = @"
public class Foo
{
    private Disposable[] disposables;

    public Foo()
    {
        var items = new Disposable[2];
        for (var i = 0; i < 2; i++)
        {
            var item = new Disposable();
            items[i] = item;
        }

        this.disposables = items;
    }
}";

            await this.VerifyHappyPathAsync(DisposableCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoresRecursiveProperty()
        {
            var testCode = @"
using System;

public class Foo
{
    public IDisposable RecursiveProperty => RecursiveProperty;

    public void Meh()
    {
        var item = RecursiveProperty;

        using(var item = RecursiveProperty)
        {
        }

        using(RecursiveProperty)
        {
        }
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoresRecursiveMethod()
        {
            var testCode = @"
using System;

public class Foo
{
    public IDisposable RecursiveMethod() => RecursiveMethod();

    public void Meh()
    {
        var meh = RecursiveMethod();

        using(var item = RecursiveMethod())
        {
        }

        using(RecursiveMethod())
        {
        }
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}