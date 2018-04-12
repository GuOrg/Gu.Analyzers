namespace Gu.Analyzers
{
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
