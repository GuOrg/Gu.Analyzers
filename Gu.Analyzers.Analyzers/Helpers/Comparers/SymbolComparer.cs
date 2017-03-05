namespace Gu.Analyzers
{
    using System.Collections.Generic;

    using Microsoft.CodeAnalysis;

    internal class SymbolComparer : IEqualityComparer<ISymbol>
    {
        internal static readonly SymbolComparer Default = new SymbolComparer();

        private SymbolComparer()
        {
        }

        public static bool Equals(ISymbol x, ISymbol y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null ||
                y == null)
            {
                return false;
            }

            return DefinitionEquals(x, y) ||
                   DefinitionEquals(y, x) ||
                   Equals((x as IPropertySymbol)?.OverriddenProperty, y) ||
                   Equals(x, (y as IPropertySymbol)?.OverriddenProperty) ||
                   Equals((x as IMethodSymbol)?.OverriddenMethod, y) ||
                   Equals(x, (y as IMethodSymbol)?.OverriddenMethod);
        }

        bool IEqualityComparer<ISymbol>.Equals(ISymbol x, ISymbol y) => Equals(x, y);

        public int GetHashCode(ISymbol obj)
        {
            return obj?.MetadataName.GetHashCode() ?? 0;
        }

        private static bool DefinitionEquals(ISymbol x, ISymbol y)
        {
            if (x.IsDefinition && !y.IsDefinition &&
                !ReferenceEquals(y, y.OriginalDefinition))
            {
                return Equals(x, y.OriginalDefinition);
            }

            return false;
        }
    }
}
