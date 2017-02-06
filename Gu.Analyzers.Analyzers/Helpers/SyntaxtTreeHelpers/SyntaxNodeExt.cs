namespace Gu.Analyzers
{
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        internal static bool IsBeforeInScope(this SyntaxNode node, SyntaxNode other)
        {
            var statement = node?.FirstAncestorOrSelf<StatementSyntax>();
            var otherStatement = other?.FirstAncestorOrSelf<StatementSyntax>();
            if (statement == null ||
                otherStatement == null)
            {
                return false;
            }

            if (statement.SpanStart >= otherStatement.SpanStart)
            {
                return false;
            }

            var block = node.FirstAncestor<BlockSyntax>();
            var otherblock = other.FirstAncestor<BlockSyntax>();
            if (block == null || otherblock == null)
            {
                return false;
            }

            while (otherblock != null)
            {
                if (ReferenceEquals(block, otherblock))
                {
                    return true;
                }

                otherblock = otherblock.FirstAncestor<BlockSyntax>();
            }

            return false;
        }
    }
}
