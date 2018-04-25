namespace Gu.Analyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class Constructor
    {
        internal static bool TryGetConstructor(this ObjectCreationExpressionSyntax objectCreation, QualifiedType qualifiedType, SemanticModel semanticModel, CancellationToken cancellationToken, out IMethodSymbol ctor)
        {
            if (objectCreation.Type is SimpleNameSyntax typeName &&
                (typeName.Identifier.ValueText == qualifiedType.Type ||
                 AliasWalker.Contains(objectCreation.SyntaxTree, typeName.Identifier.ValueText)))
            {
                ctor = semanticModel.GetSymbolSafe(objectCreation, cancellationToken);
                return ctor?.ContainingType == qualifiedType;
            }

            if (objectCreation.Type is QualifiedNameSyntax qualifiedName &&
                qualifiedName.Right.Identifier.ValueText == qualifiedType.Type)
            {
                ctor = semanticModel.GetSymbolSafe(objectCreation, cancellationToken);
                return ctor?.ContainingType == qualifiedType;
            }

            ctor = null;
            return false;
        }

        internal static bool Creates(this ObjectCreationExpressionSyntax creation, ConstructorDeclarationSyntax ctor, Search search, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var created = semanticModel.GetSymbolSafe(creation, cancellationToken);
            var ctorSymbol = semanticModel.GetDeclaredSymbolSafe(ctor, cancellationToken);
            if (SymbolComparer.Equals(ctorSymbol, created))
            {
                return true;
            }

            return search == Search.Recursive &&
                   IsRunBefore(created, ctorSymbol, semanticModel, cancellationToken);
        }

        internal static bool IsRunBefore(this ConstructorDeclarationSyntax ctor, ConstructorDeclarationSyntax otherDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (ctor == otherDeclaration)
            {
                return false;
            }

            var first = semanticModel.GetDeclaredSymbolSafe(ctor, cancellationToken);
            var other = semanticModel.GetDeclaredSymbolSafe(otherDeclaration, cancellationToken);
            return IsRunBefore(first, other, semanticModel, cancellationToken);
        }

        internal static bool IsRunBefore(IMethodSymbol first, IMethodSymbol other, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (first == null ||
                other == null)
            {
                return false;
            }

            if (TryGetInitializer(other, cancellationToken, out ConstructorInitializerSyntax initializer))
            {
                if (SymbolComparer.Equals(first.ContainingType, other.ContainingType) &&
                    !initializer.ThisOrBaseKeyword.IsKind(SyntaxKind.ThisKeyword))
                {
                    return false;
                }

                if (!other.ContainingType.Is(first.ContainingType) &&
                    initializer.ThisOrBaseKeyword.IsKind(SyntaxKind.BaseKeyword))
                {
                    return false;
                }
            }
            else
            {
                if (SymbolComparer.Equals(first.ContainingType, other.ContainingType) ||
                    !other.ContainingType.Is(first.ContainingType))
                {
                    return false;
                }
            }

            var next = semanticModel.GetSymbolSafe(initializer, cancellationToken);
            if (SymbolComparer.Equals(first, next))
            {
                return true;
            }

            if (next == null)
            {
                if (TryGetDefault(other.ContainingType?.BaseType, out next))
                {
                    return SymbolComparer.Equals(first, next);
                }

                return false;
            }

            return IsRunBefore(first, next, semanticModel, cancellationToken);
        }

        internal static bool TryGetDefault(INamedTypeSymbol type, out IMethodSymbol result)
        {
            result = null;
            while (type != null && type != KnownSymbol.Object)
            {
                bool found = false;
                foreach (var ctorSymbol in type.Constructors)
                {
                    if (ctorSymbol.Parameters.Length == 0)
                    {
                        found = true;
                        if (ctorSymbol.DeclaringSyntaxReferences.Length != 0)
                        {
                            result = ctorSymbol;
                            return true;
                        }
                    }
                }

                if (!found)
                {
                    return false;
                }

                type = type.BaseType;
            }

            return false;
        }

        private static bool TryGetInitializer(IMethodSymbol ctor, CancellationToken cancellationToken, out ConstructorInitializerSyntax initializer)
        {
            initializer = null;
            if (ctor == null ||
                ctor.MethodKind != MethodKind.Constructor)
            {
                return false;
            }

            foreach (var reference in ctor.DeclaringSyntaxReferences)
            {
                var declaration = (ConstructorDeclarationSyntax)reference.GetSyntax(cancellationToken);
                initializer = declaration.Initializer;
                if (initializer != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
