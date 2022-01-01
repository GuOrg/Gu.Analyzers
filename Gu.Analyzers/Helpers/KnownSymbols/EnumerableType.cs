namespace Gu.Analyzers;

using Gu.Roslyn.AnalyzerExtensions;

internal class EnumerableType : QualifiedType
{
    internal readonly QualifiedMethod FirstOrDefault;
    internal readonly QualifiedMethod LastOrDefault;
    internal readonly QualifiedMethod SingleOrDefault;

    internal EnumerableType()
        : base("System.Linq.Enumerable")
    {
        this.FirstOrDefault = new QualifiedMethod(this, nameof(this.FirstOrDefault));
        this.LastOrDefault = new QualifiedMethod(this, nameof(this.LastOrDefault));
        this.SingleOrDefault = new QualifiedMethod(this, nameof(this.SingleOrDefault));
    }
}
