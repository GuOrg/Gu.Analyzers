namespace Gu.Analyzers;

using Gu.Roslyn.AnalyzerExtensions;

internal class NUnitAssertType : QualifiedType
{
    internal readonly QualifiedMethod AreEqual;

    internal NUnitAssertType()
        : base("NUnit.Framework.Assert")
    {
        this.AreEqual = new QualifiedMethod(this, nameof(this.AreEqual));
    }
}