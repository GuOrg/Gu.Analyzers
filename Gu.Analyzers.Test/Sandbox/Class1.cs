// ReSharper disable All
namespace Gu.Analyzers.Test.Sandbox
{
    using System.IO;

    public static class Foo
    {
        public static long Bar()
        {
            var stream = GetStream();
            return stream.Length;
        }

        public static Stream GetStream() => File.OpenRead("");
    }
}
