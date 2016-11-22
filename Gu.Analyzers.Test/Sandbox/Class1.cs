namespace Gu.Analyzers.Test.Sandbox
{
    using System;

    public class Foo
    {
        public Foo(object o)
        {
            throw new ArgumentException("message", nameof(o), new Exception());
        }
    }
}
