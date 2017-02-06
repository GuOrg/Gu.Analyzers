namespace Gu.Analyzers.Test
{
    using System;
    using System.Linq;

    using Microsoft.CodeAnalysis;

    internal static class SyntaxNodeExt
    {
        internal static T FirstDescendant<T>(this SyntaxTree tree, string text)
            where T : SyntaxNode
        {
            foreach (var node in tree.GetRoot().DescendantNodes().OfType<T>())
            {
                if (node.ToString() == text)
                {
                    return node;
                }
            }

            throw new InvalidOperationException($"The tree does not contain a {typeof(T).Name} with text {text}");
        }

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
    }
}