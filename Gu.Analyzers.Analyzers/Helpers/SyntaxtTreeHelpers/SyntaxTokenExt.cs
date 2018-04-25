namespace Gu.Analyzers
{
    using System;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    /// <summary>
    /// Helpers for working with <see cref="SyntaxNode"/>
    /// </summary>
    [Obsolete("Use extensions")]
    internal static class SyntaxTokenExt
    {
        internal static bool IsEither(this SyntaxToken node, SyntaxKind sk1, SyntaxKind sk2) => node.IsKind(sk1) || node.IsKind(sk2);

        internal static FileLinePositionSpan FileLinePositionSpan(this SyntaxToken token, CancellationToken cancellationToken)
        {
            return token.SyntaxTree.GetLineSpan(token.Span, cancellationToken);
        }

        /// <summary>
        /// Get the <see cref="Microsoft.CodeAnalysis.FileLinePositionSpan"/> for the token in the containing document.
        /// </summary>
        /// <param name="node">The <see cref="SyntaxNode"/></param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>The <see cref="Microsoft.CodeAnalysis.FileLinePositionSpan"/> for the token in the containing document.</returns>
        internal static FileLinePositionSpan FileLinePositionSpan(this SyntaxNode node, CancellationToken cancellationToken)
        {
            return node.SyntaxTree.GetLineSpan(node.Span, cancellationToken);
        }
    }
}
