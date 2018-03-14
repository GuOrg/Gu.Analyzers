namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    internal static class SyntaxTokenExt
    {
        internal static bool IsEither(this SyntaxToken node, SyntaxKind sk1, SyntaxKind sk2) => node.IsKind(sk1) || node.IsKind(sk2);
    }
}