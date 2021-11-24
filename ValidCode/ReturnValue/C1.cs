#pragma warning disable CA1822 // Mark members as static
namespace ValidCode.ReturnValue
{
    internal class C1
    {
        internal void M1()
        {
            using var c1 = new C().M1(typeof(C));
            using var c2 = new C();
            c2.M1(typeof(C));
        }

        internal void M2()
        {
            using var c1 = new C().M2(typeof(C));
            using var c2 = new C();
            c2.M2(typeof(C));
        }
    }
}
