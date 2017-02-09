namespace Gu.Analyzers.Test
{
    using System;
    using System.Linq;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class SyntaxNodeExt
    {
        [Obsolete("Use EqualsValueClause")]
        internal static T Descendant<T>(this SyntaxTree tree, int index = 0)
            where T : SyntaxNode
        {
            var count = 0;
            foreach (var node in tree.GetRoot().DescendantNodes().OfType<T>())
            {
                if (count == index)
                {
                    return node;
                }

                count++;
            }

            throw new InvalidOperationException($"The tree does not contain a {typeof(T).Name} with index {index}");
        }

        internal static EqualsValueClauseSyntax EqualsValueClause(this SyntaxTree tree, string statement)
        {
            foreach (var node in tree.GetRoot().DescendantNodes().OfType<EqualsValueClauseSyntax>())
            {
                var statementSyntax = node.FirstAncestor<StatementSyntax>();
                if (statementSyntax?.ToFullString().Contains(statement) == true)
                {
                    return node;
                }
            }

            throw new InvalidOperationException($"The tree does not contain an {typeof(EqualsValueClauseSyntax).Name} in a statement: {statement}");
        }

        internal static AssignmentExpressionSyntax AssignmentExpression(this SyntaxTree tree, string statement)
        {
            foreach (var node in tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>())
            {
                var statementSyntax = node.FirstAncestor<StatementSyntax>();
                if (statementSyntax?.ToFullString()?.Contains(statement) == true)
                {
                    return node;
                }
            }

            throw new InvalidOperationException($"The tree does not contain an {typeof(AssignmentExpressionSyntax).Name} in a statement: {statement}");
        }
    }
}