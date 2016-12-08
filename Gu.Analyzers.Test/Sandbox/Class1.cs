// ReSharper disable All
namespace Gu.Analyzers.Test.Sandbox
{
    using System.Collections.Generic;
    using System.IO;

    public class Foo
    {
        private readonly List<Stream> streams = new List<Stream>();

        public void Bar()
        {
            var stream = File.OpenRead("");
            this.streams.Add(stream);
        }
    }
}
