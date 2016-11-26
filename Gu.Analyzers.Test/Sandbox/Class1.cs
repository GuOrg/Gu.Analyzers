namespace Gu.Analyzers.Test.Sandbox
{
    public class Foo
    {
        public int A { get; private set; }

        public void Meh()
        {
            var foo = new Foo();
            foo.A = A;
        }
    }
}
