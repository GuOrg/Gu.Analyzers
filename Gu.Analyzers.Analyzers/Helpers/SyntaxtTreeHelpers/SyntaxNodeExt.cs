namespace Gu.Analyzers
{
    using System.Threading;

    using Microsoft.CodeAnalysis;

    internal static class SyntaxNodeExt
    {
        internal static int StartingLineNumber(this SyntaxNode node, CancellationToken cancellationToken)
        {
            return node.SyntaxTree.GetLineSpan(node.Span, cancellationToken).Span.Start.Line;
        }

        internal static int StartingLineNumber(this SyntaxToken token, CancellationToken cancellationToken)
        {
            return token.SyntaxTree.GetLineSpan(token.Span, cancellationToken).Span.Start.Line;
        }

        internal static T FirstAncestor<T>(this SyntaxNode node)
            where T : SyntaxNode
        {
            if (node == null)
            {
                return null;
            }

            var ancestor = node.FirstAncestorOrSelf<T>();
            return ReferenceEquals(ancestor, node)
                       ? node.Parent?.FirstAncestorOrSelf<T>()
                       : ancestor;
        }
    }
}
