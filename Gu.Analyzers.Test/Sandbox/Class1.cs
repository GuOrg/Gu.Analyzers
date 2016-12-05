namespace Gu.Analyzers.Test.Sandbox
{
    using System;
    using System.IO;

    public static class Foo
    {
        public static long Bar()
        {
            using (var stream = File.OpenRead(""))
            {
                return stream.Length;
            }
        }
    }
}
