namespace ValidCode
{
    using System;
    using System.Threading.Tasks;

    internal struct Bar : IDisposable
    {
        internal Bar(Task task)
        {
            this.Task = task;
        }

        internal Task Task { get; }

        public void Dispose()
        {
        }
    }
}
