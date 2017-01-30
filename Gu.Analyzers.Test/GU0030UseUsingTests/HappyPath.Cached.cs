namespace Gu.Analyzers.Test.GU0030UseUsingTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class HappyPath
    {
        public class Cached : NestedHappyPathVerifier<HappyPath>
        {
            [Test]
            public async Task DontUseUsingWhenGettingFromConcurrentDictionaryGetOrAdd()
            {
                var testCode = @"
using System.Collections.Concurrent;
using System.IO;

public static class Foo
{
    private static readonly ConcurrentDictionary<int, Stream> Cache = new ConcurrentDictionary<int, Stream>();

    public static long Bar()
    {
        var stream = Cache.GetOrAdd(1, _ => File.OpenRead(string.Empty));
        return stream.Length;
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task DontUseUsingWhenGettingFromConcurrentDictionaryTryGetValue()
            {
                var testCode = @"
using System.Collections.Concurrent;
using System.IO;

public static class Foo
{
    private static readonly ConcurrentDictionary<int, Stream> Cache = new ConcurrentDictionary<int, Stream>();

    public static long Bar()
    {
        Stream stream;
        if (Cache.TryGetValue(1, out stream))
        {
            return stream.Length;
        }

        return 0;
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task DontUseUsingWhenGettingFromConditionalWeakTableTryGetValue()
            {
                var testCode = @"
using System.IO;
using System.Runtime.CompilerServices;

public static class Foo
{
    private static readonly ConditionalWeakTable<string, Stream> Cache = new ConditionalWeakTable<string, Stream>();

    public static long Bar()
    {
        Stream stream;
        if (Cache.TryGetValue(""1"", out stream))
        {
            return stream.Length;
        }

        return 0;
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
    private static readonly Stream Stream = File.OpenRead(string.Empty);

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
            public async Task DontUseUsingWhenAssigningACallThatReturnsAFieldSwitch()
            {
                var testCode = @"
using System;
using System.IO;

public static class Foo
{
    private static readonly Stream Stream = File.OpenRead(string.Empty);

    public static long Bar()
    {
        var stream = GetStream(FileAccess.Read);
        return stream.Length;
    }

    public static Stream GetStream(FileAccess fileAccess)
    {
        switch (fileAccess)
        {
            case FileAccess.Read:
                return Stream;
            case FileAccess.Write:
                return Stream;
            case FileAccess.ReadWrite:
                return Stream;
            default:
                throw new ArgumentOutOfRangeException(nameof(fileAccess), fileAccess, null);
        }
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }
        }
    }
}