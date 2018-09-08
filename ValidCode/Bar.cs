namespace ValidCode
{
    using System;
    using System.Threading.Tasks;

    internal struct Bar : IDisposable
    {
        public Bar(Task task)
        {
            this.Task = task;
        }

        public Task Task { get; }

        public void Dispose()
        {
        }
    }
}
