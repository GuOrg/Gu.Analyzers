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
                AccessorDeclarationSyntax setter;
                if (declaration.TryGetSetAccessorDeclaration(out setter))
                {
                    if (AssignmentWalker.Assigns(symbol, setter, semanticModel, cancellationToken))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
