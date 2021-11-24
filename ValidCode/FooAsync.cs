﻿// ReSharper disable All
namespace ValidCode
{
    using System.IO;
    using System.Threading.Tasks;

    internal static class FooAsync
    {
        internal static async Task<string?> Bar1Async()
        {
            using var stream = await ReadAsync(string.Empty);
            using var reader = new StreamReader(stream);
            return reader.ReadLine();
        }

        internal static async Task<string?> Bar2Async()
        {
            using var stream = await ReadAsync(string.Empty).ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            return reader.ReadLine();
        }

        private static async Task<Stream> ReadAsync(this string fileName)
        {
            var stream = new MemoryStream();
            using (var fileStream = File.OpenRead(fileName))
            {
                await fileStream.CopyToAsync(stream)
                                .ConfigureAwait(false);
            }

            stream.Position = 0;
            return stream;
        }
    }
}
