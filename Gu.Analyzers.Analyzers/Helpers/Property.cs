namespace Gu.Analyzers
{
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class Property
    {
        internal static bool TryGetSetter(this IPropertySymbol property, CancellationToken cancellationToken, out AccessorDeclarationSyntax setter)
        {
            setter = null;
            if (property == null)
            {
                return false;
            }

            foreach (var reference in property.DeclaringSyntaxReferences)
            {
                var propertyDeclaration = reference.GetSyntax(cancellationToken) as PropertyDeclarationSyntax;
                if (BasePropertyDeclarationSyntaxExt.TryGetSetter(propertyDeclaration, out setter))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
