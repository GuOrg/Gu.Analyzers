// ReSharper disable All
namespace ValidCode
{
    using System;
    using System.IO;

    internal class FooOut
    {
        internal static bool TryGetStream(out Stream stream)
        {
            return TryGetStreamCore(out stream);
        }

        internal void Bar()
        {
            IDisposable disposable;
            using (disposable = new Disposable())
            {
            }
        }

        internal void Baz()
        {
            Stream disposable;
            if (TryGetStreamCore(out disposable))
            {
                using (disposable)
                {
                }
            }
        }

        private static bool TryGetStreamCore(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}
