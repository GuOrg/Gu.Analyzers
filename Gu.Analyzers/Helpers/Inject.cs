namespace Gu.Analyzers
{
    using System.Diagnostics.CodeAnalysis;
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

        internal static bool TryFindConstructor(SyntaxNode node, [NotNullWhen(true)]out ConstructorDeclarationSyntax? ctor)
        {
            ctor = null;
            return node.TryFirstAncestor(out ClassDeclarationSyntax? classDeclaration) &&
                   !classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword) &&
                   classDeclaration.Members.TrySingleOfType<MemberDeclarationSyntax, ConstructorDeclarationSyntax>(x => !x.Modifiers.Any(SyntaxKind.StaticKeyword), out ctor) &&
                   !ctor.Modifiers.Any(SyntaxKind.PrivateKeyword);
        }
    }
}
