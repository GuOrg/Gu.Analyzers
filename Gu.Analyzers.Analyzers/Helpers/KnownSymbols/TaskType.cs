namespace Gu.Analyzers
{
    internal class TaskType : QualifiedType
    {
        internal readonly QualifiedMethod FromResult;

        internal TaskType()
            : base("System.Threading.Tasks.Task")
        {
            this.FromResult = new QualifiedMethod(this, nameof(this.FromResult));
        }
    }
}