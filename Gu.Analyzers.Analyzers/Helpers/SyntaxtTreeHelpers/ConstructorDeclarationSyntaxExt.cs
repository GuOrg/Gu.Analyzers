namespace Gu.Analyzers
{
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class ConstructorDeclarationSyntaxExt
    {
        internal static bool IsRunBefore(
            this ConstructorDeclarationSyntax first,
            ConstructorDeclarationSyntax other,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (first == null ||
                other == null)
            {
                return false;
            }

            var firstSymbol = semanticModel.GetDeclaredSymbolSafe(first, cancellationToken);
            var otherSymbol = semanticModel.GetDeclaredSymbolSafe(other, cancellationToken);
            if (ReferenceEquals(firstSymbol, otherSymbol))
            {
                return false;
            }

            if (other.Initializer == null)
            {
                if (firstSymbol.Parameters.Length != 0 ||
                    firstSymbol.ContainingType == otherSymbol.ContainingType)
                {
                    return false;
                }

                return otherSymbol.ContainingType.Is(firstSymbol.ContainingType);
            }

            var initializerSymbol = semanticModel.GetSymbolSafe(other.Initializer, cancellationToken);
            if (ReferenceEquals(initializerSymbol, firstSymbol))
            {
                return true;
            }

            foreach (var reference in initializerSymbol.DeclaringSyntaxReferences)
            {
                if (IsRunBefore(
                    first,
                    (ConstructorDeclarationSyntax)reference.GetSyntax(cancellationToken),
                    semanticModel,
                    cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }
    }
}