namespace Gu.Analyzers
{
    using System;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;

    internal static class SemanticModelExt
    {
        [Obsolete("Will be in Gu.Roslyn.Extensions")]
        internal static bool TryGetNamedType(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken, out INamedTypeSymbol namedType)
        {
            if (semanticModel.TryGetType(node, cancellationToken, out var candidate))
            {
                namedType = candidate as INamedTypeSymbol;
                return namedType != null;
            }

            namedType = null;
            return false;
        }
    }
}
