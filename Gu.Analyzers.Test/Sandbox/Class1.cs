namespace Gu.Analyzers.Test.Sandbox
{
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
            this.Bar = new Foo(this.A, this.B, this.C, this.D);
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }

        public Foo Bar { get; }
    }
}
