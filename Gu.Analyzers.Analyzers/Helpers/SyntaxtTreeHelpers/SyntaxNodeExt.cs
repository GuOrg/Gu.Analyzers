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

            var block = statement.Parent as BlockSyntax;
            var otherblock = otherStatement.Parent as BlockSyntax;
            if (block == null || otherblock == null)
            {
                if (SharesAncestor<IfStatementSyntax>(statement, otherStatement) ||
                    SharesAncestor<SwitchStatementSyntax>(statement, otherStatement))
                {
                    return false;
                }
            }

            block = statement.FirstAncestor<BlockSyntax>();
            otherblock = otherStatement.FirstAncestor<BlockSyntax>();
            if (block == null || otherblock == null)
            {
                return false;
            }

            if (ReferenceEquals(block, otherblock) ||
                otherblock.Span.Contains(block.Span) ||
                block.Span.Contains(otherblock.Span))
            {
                return true;
            }

            return false;
        }

        internal static bool SharesAncestor<T>(this SyntaxNode first, SyntaxNode other)
            where T : SyntaxNode
        {
            var firstAncestor = first.FirstAncestor<T>();
            var otherAncestor = other.FirstAncestor<T>();
            if (firstAncestor == null ||
                otherAncestor == null)
            {
                return false;
            }

            return firstAncestor == otherAncestor;
        }
    }
}
