namespace Gu.Analyzers
{
    using System.Threading;
    using Microsoft.CodeAnalysis;

    internal static class SymbolExt
    {
        internal static bool TryGetSingleDeclaration<T>(this ISymbol symbol, CancellationToken cancellationToken, out T declaration)
            where T : SyntaxNode
        {
            declaration = null;
            if (symbol == null)
            {
                return false;
            }

            SyntaxReference syntaxReference;
            if (symbol.DeclaringSyntaxReferences.TryGetSingle(out syntaxReference))
            {
                declaration = syntaxReference.GetSyntax(cancellationToken) as T;
                return declaration != null;
            }

            return false;
        }
    }
}
