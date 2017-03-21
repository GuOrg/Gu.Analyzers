namespace Gu.Analyzers
{
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class Property
    {
        internal static bool AssignsSymbolInSetter(IPropertySymbol property, ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var setMethod = property?.SetMethod;
            if (setMethod == null ||
                setMethod.DeclaringSyntaxReferences.Length == 0)
            {
                return false;
            }

            foreach (var reference in property.DeclaringSyntaxReferences)
            {
                var declaration = (PropertyDeclarationSyntax)reference.GetSyntax(cancellationToken);
                if (declaration.TryGetSetAccessorDeclaration(out AccessorDeclarationSyntax setter))
                {
                    if (Assigns.FirstSymbol(symbol, setter, true, semanticModel, cancellationToken))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool IsAutoProperty(this IPropertySymbol propertySymbol, CancellationToken cancellationToken)
        {
            if (propertySymbol == null)
            {
                return false;
            }

            foreach (var reference in propertySymbol.DeclaringSyntaxReferences)
            {
                var declaration = (BasePropertyDeclarationSyntax)reference.GetSyntax(cancellationToken);
                if ((declaration as PropertyDeclarationSyntax)?.ExpressionBody != null)
                {
                    return false;
                }

                if (declaration.TryGetGetAccessorDeclaration(out AccessorDeclarationSyntax getter) &&
                    getter.Body == null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
