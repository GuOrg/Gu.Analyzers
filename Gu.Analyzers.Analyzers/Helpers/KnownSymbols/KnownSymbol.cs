namespace Gu.Analyzers
{
    internal static class KnownSymbol
    {
        internal static readonly QualifiedType Object = Create("System.Object");

        private static QualifiedType Create(string qualifiedName)
        {
            return new QualifiedType(qualifiedName);
        }
    }
}