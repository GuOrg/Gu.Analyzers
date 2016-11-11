namespace Gu.Analyzers
{
    internal static class KnownSymbol
    {
        internal static readonly QualifiedType Object = Create("System.Object");
        internal static readonly QualifiedType Tuple = Create("System.Tuple");
        internal static readonly QualifiedType Expression = Create("System.Linq.Expressions.Expression");
        internal static readonly StringType String = new StringType();
        internal static readonly DependencyPropertyType DependencyProperty = new DependencyPropertyType();

        private static QualifiedType Create(string qualifiedName)
        {
            return new QualifiedType(qualifiedName);
        }
    }
}