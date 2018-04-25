namespace Gu.Analyzers
{
    using Gu.Roslyn.AnalyzerExtensions;

    internal class MockOfTType : QualifiedType
    {
        internal readonly QualifiedMethod Setup;

        public MockOfTType()
            : base("Moq.Mock`1")
        {
            this.Setup = new QualifiedMethod(this, nameof(this.Setup));
        }
    }
}
