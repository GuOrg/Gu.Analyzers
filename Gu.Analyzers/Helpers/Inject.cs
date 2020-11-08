namespace Gu.Analyzers
{
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class Inject
    {
        internal enum Injectable
        {
            No,
            Safe,
            Unsafe,
        }

        internal static ConstructorDeclarationSyntax? FindConstructor(SyntaxNode node)
        {
            if (node.TryFirstAncestor(out ClassDeclarationSyntax? classDeclaration) &&
                !classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword) &&
                classDeclaration.Members.TrySingleOfType<MemberDeclarationSyntax, ConstructorDeclarationSyntax>(x => !x.Modifiers.Any(SyntaxKind.StaticKeyword), out var ctor) &&
                !ctor.Modifiers.Any(SyntaxKind.PrivateKeyword))
            {
                return ctor;
            }

            return null;
        }
    }
}
