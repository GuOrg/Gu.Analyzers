namespace Gu.Analyzers.Test.Sandbox
{
    public class Foo
    {
        public Foo(int a, int b)
            : this(b)
        {
            this.A = a;
        }

        public Foo(int b)
        {
            this.B = b;
        }

        public int A { get; }

        public int B { get; }
    }
}
