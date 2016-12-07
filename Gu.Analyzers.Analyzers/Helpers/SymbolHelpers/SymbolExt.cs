namespace Gu.Analyzers.Helpers.SymbolHelpers
{
    using System.Threading;
    using Microsoft.CodeAnalysis;

    internal static class SymbolExt
    {
        internal static bool TryGetDeclaration<T>(this ISymbol symbol, CancellationToken cancellationToken, out T declaration)
            where T : SyntaxNode
        {
            SyntaxReference syntaxReference;
            if (symbol.DeclaringSyntaxReferences.TryGetSingle(out syntaxReference))
            {
                declaration = (T)syntaxReference.GetSyntax(cancellationToken);
                return declaration != null;
            }

            declaration = null;
            return false;
        }
    }
}
