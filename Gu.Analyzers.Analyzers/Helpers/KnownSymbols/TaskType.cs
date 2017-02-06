namespace Gu.Analyzers
{
    internal class TaskType : QualifiedType
    {
        internal readonly QualifiedMethod FromResult;
        internal readonly QualifiedMethod Run;

        internal TaskType()
            : base("System.Threading.Tasks.Task")
        {
            this.FromResult = new QualifiedMethod(this, nameof(this.FromResult));
            this.Run = new QualifiedMethod(this, nameof(this.Run));
        }
    }
}