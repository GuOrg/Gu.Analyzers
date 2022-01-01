namespace Gu.Analyzers;

using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class BasePropertyDeclarationSyntaxExt
{
    internal static bool IsPropertyOrIndexer(this BasePropertyDeclarationSyntax declaration)
    {
        return declaration is PropertyDeclarationSyntax || declaration is IndexerDeclarationSyntax;
    }
}