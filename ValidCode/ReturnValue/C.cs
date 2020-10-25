namespace ValidCode.ReturnValue
{
    using System;

    internal class C : IDisposable
    {
        internal C M1(Type t) => this;
        internal C M2(Type t)
        {
            this.M1(t);
            return this;
        }

        public void Dispose()
        {
        }
    }
}
