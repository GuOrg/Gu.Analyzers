namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class BasePropertyDeclarationSyntaxExt
    {
        internal static Accessibility GetAccessibility(this BasePropertyDeclarationSyntax declaration)
        {
            if (declaration == null)
            {
                return Accessibility.NotApplicable;
            }

            if (declaration.Modifiers.Any(SyntaxKind.PrivateKeyword))
            {
                return Accessibility.Private;
            }

            if (declaration.Modifiers.Any(SyntaxKind.PublicKeyword))
            {
                return Accessibility.Public;
            }

            if (declaration.Modifiers.Any(SyntaxKind.ProtectedKeyword) &&
                declaration.Modifiers.Any(SyntaxKind.InternalKeyword))
            {
                return Accessibility.ProtectedAndInternal;
            }

            if (declaration.Modifiers.Any(SyntaxKind.InternalKeyword))
            {
                return Accessibility.Internal;
            }

            if (declaration.Modifiers.Any(SyntaxKind.ProtectedKeyword))
            {
                return Accessibility.Protected;
            }

            if (declaration.ExplicitInterfaceSpecifier != null)
            {
                // This will not always be right.
                return Accessibility.Public;
            }

            return Accessibility.Internal;
        }

        internal static bool IsPropertyOrIndexer(this BasePropertyDeclarationSyntax declaration)
        {
            return declaration is PropertyDeclarationSyntax || declaration is IndexerDeclarationSyntax;
        }
   }
}
