namespace Gu.Analyzers;

using System;
using System.Linq;
using Gu.Roslyn.AnalyzerExtensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

[Obsolete("Use Gu.Roslyn.Extensions")]
internal static class Temp
{
    /// <summary>
    /// Add leading line feed to <paramref name="node"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="SyntaxNode"/>.</typeparam>
    /// <param name="node">The <typeparamref name="T"/>.</param>
    /// <returns><paramref name="node"/> with leading line feed.</returns>
    internal static T WithoutTrailingLineFeed<T>(this T node)
        where T : SyntaxNode
    {
        if (node is null)
        {
            throw new System.ArgumentNullException(nameof(node));
        }

        if (node.HasTrailingTrivia &&
            node.GetTrailingTrivia() is { } triviaList &&
            triviaList.TryLast(out var first) &&
            first.IsKind(SyntaxKind.EndOfLineTrivia))
        {
            return node.WithTrailingTrivia(triviaList.Take(triviaList.Count - 2));
        }

        return node;
    }
}
