// ReSharper disable All
#pragma warning disable 1717
#pragma warning disable SA1101 // Prefix local calls with this
#pragma warning disable GU0011 // Don't ignore the return value.
#pragma warning disable GU0010 // Assigning same value.
#pragma warning disable IDE0009 // Member access should be qualified.
#pragma warning disable IDE0018 // Inline variable declaration
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
