namespace Gu.Analyzers.Test.Sandbox
{
    public class Foo
    {
        public Foo(int a, int b)
        {
            this.A = a;
            this.B = b;
        }

        public int A { get; }

        public int B { get; }

        public int C => A;

        public int D
        {
            get
            {
                return A;
            }
        }

        public int E => B;

        public int F { get; private set; }

        public int G { get; private set; }

        public int H { get; set; }

        public int I { get; set; }
    }
}
