namespace Gu.Analyzers.Test.Sandbox
{
    public class Foo
    {
        public int A { get; private set; }

        public Foo Clone()
        {
            return new Foo { A = A };
        }
    }
}
