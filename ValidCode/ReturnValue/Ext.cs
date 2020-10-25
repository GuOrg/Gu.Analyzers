namespace ValidCode.ReturnValue
{
    using System;

    internal static class Ext
    {
        internal static C M1Ext(this C c, Type t) => c;
        internal static C M2Ext(this C c, Type t)
        {
            return c;
        }

        internal static void M1()
        {
            using var c1 = new C().M1Ext(typeof(C));
            using var c2 = new C();
            c2.M1Ext(typeof(C));
        }

        internal static void M2()
        {
            using var c1 = new C().M2Ext(typeof(C));
            using var c2 = new C();
            c2.M2Ext(typeof(C));
        }
    }
}
