namespace Gu.Analyzers.Test.HappyPathCode
{
    using System.Threading.Tasks;

    internal sealed class Foo
    {
        public Foo()
        {
            using (new Bar(Task.Run(() => 1)))
            {
            }
        }
    }
}
