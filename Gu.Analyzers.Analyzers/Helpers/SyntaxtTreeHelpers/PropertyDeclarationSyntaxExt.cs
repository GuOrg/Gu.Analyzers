namespace Gu.Analyzers
{
    using System;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class PropertyDeclarationSyntaxExt
    {
        [Obsolete("Remove")]
        internal static string Name(this PropertyDeclarationSyntax property)
        {
            return property?.Identifier.ValueText;
        }

        internal static SyntaxToken Identifier(this PropertyDeclarationSyntax property)
        {
            return property.Identifier;
        }
    }
}