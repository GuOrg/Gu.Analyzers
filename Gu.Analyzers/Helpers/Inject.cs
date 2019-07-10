namespace Gu.Analyzers
{
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class Inject
    {
        internal static bool TryFindConstructor(SyntaxNode node, out ConstructorDeclarationSyntax ctor)
        {
            ctor = null;
            return node.TryFirstAncestor(out ClassDeclarationSyntax classDeclaration) &&
                   classDeclaration.Members.TrySingleOfType(x => !x.Modifiers.Any(SyntaxKind.StaticKeyword), out ctor) &&
                   !ctor.Modifiers.Any(SyntaxKind.PrivateKeyword);
        }

    }
}
