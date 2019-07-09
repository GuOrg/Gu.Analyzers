// ReSharper disable All
namespace ValidCode
{
    using System.Threading.Tasks;

    internal sealed class Foo
    {
        internal Foo()
        {
            using (new Bar(Task.Run(() => 1)))
            {
            }
        }
    }
}
