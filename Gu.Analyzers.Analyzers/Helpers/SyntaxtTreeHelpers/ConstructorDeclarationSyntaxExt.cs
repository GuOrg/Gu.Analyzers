namespace Gu.Analyzers
{
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class ConstructorDeclarationSyntaxExt
    {
        internal static bool IsBeforeInScope(this ConstructorDeclarationSyntax first, ConstructorDeclarationSyntax other, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (first == null ||
                other == null)
            {
                return false;
            }

            var firstSymbol = semanticModel.GetDeclaredSymbolSafe(first, cancellationToken);
            var otherSymbol = semanticModel.GetDeclaredSymbolSafe(other, cancellationToken);

            if (first.Initializer == null)
            {
                if (otherSymbol.Parameters.Length != 0)
                {
                    return false;
                }

                return firstSymbol.ContainingType.Is(otherSymbol.ContainingType);
            }

            var initializerSymbol = semanticModel.GetSymbolSafe(first.Initializer, cancellationToken);
            if (ReferenceEquals(initializerSymbol, otherSymbol))
            {
                return true;
            }

            foreach (var reference in initializerSymbol.DeclaringSyntaxReferences)
            {
                if (IsBeforeInScope((ConstructorDeclarationSyntax)reference.GetSyntax(cancellationToken), other, semanticModel, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }
    }
}