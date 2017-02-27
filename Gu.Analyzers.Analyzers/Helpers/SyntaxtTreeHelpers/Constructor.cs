﻿namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class Constructor
    {
        internal static bool IsCalledByOther(IMethodSymbol ctor, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (ctor == null)
            {
                return false;
            }

            foreach (var other in ctor.ContainingType.Constructors)
            {
                if (ReferenceEquals(ctor, other))
                {
                    continue;
                }

                foreach (var reference in other.DeclaringSyntaxReferences)
                {
                    var otherDeclaration = (ConstructorDeclarationSyntax)reference.GetSyntax(cancellationToken);
                    if (otherDeclaration.Initializer?.ThisOrBaseKeyword.IsKind(SyntaxKind.ThisKeyword) == true)
                    {
                        var chained = semanticModel.GetSymbolSafe(otherDeclaration.Initializer, cancellationToken);
                        if (ctor.Equals(chained))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal static bool TryGetDefault(INamedTypeSymbol type, out IMethodSymbol result)
        {
            while (type != null && type != KnownSymbol.Object)
            {
                foreach (var ctorSymbol in type.Constructors)
                {
                    if (ctorSymbol.Parameters.Length == 0)
                    {
                        result = ctorSymbol;
                        return true;
                    }
                }

                type = type.BaseType;
            }

            result = null;
            return false;
        }

        internal static void AddRunBefore(SyntaxNode context, HashSet<IMethodSymbol> ctorsRunBefore, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (context == null)
            {
                return;
            }

            var contextCtor = context.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
            if (contextCtor == null)
            {
                var type = (INamedTypeSymbol)semanticModel.GetDeclaredSymbolSafe(context.FirstAncestorOrSelf<TypeDeclarationSyntax>(), cancellationToken);
                if (type == null)
                {
                    return;
                }

                if (type.Constructors.Length != 0)
                {
                    foreach (var ctor in type.Constructors)
                    {
                        foreach (var reference in ctor.DeclaringSyntaxReferences)
                        {
                            var ctorDeclaration = (ConstructorDeclarationSyntax)reference.GetSyntax(cancellationToken);
                            ctorsRunBefore.Add(ctor).IgnoreReturnValue();
                            AddCtorsRecursively(ctorDeclaration, ctorsRunBefore, semanticModel, cancellationToken);
                        }
                    }
                }
                else
                {
                    IMethodSymbol ctor;
                    if (TryGetDefault(type, out ctor))
                    {
                        foreach (var reference in ctor.DeclaringSyntaxReferences)
                        {
                            var ctorDeclaration = (ConstructorDeclarationSyntax)reference.GetSyntax(cancellationToken);
                            ctorsRunBefore.Add(ctor).IgnoreReturnValue();
                            AddCtorsRecursively(ctorDeclaration, ctorsRunBefore, semanticModel, cancellationToken);
                        }
                    }
                }
            }
            else
            {
                AddCtorsRecursively(contextCtor, ctorsRunBefore, semanticModel, cancellationToken);
            }
        }

        private static void AddCtorsRecursively(ConstructorDeclarationSyntax ctor, HashSet<IMethodSymbol> ctorsRunBefore, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (ctor.Initializer != null)
            {
                var nestedCtor = semanticModel.GetSymbolSafe(ctor.Initializer, cancellationToken);
                if (nestedCtor == null)
                {
                    return;
                }

                foreach (var reference in nestedCtor.DeclaringSyntaxReferences)
                {
                    var runBefore = (ConstructorDeclarationSyntax)reference.GetSyntax(cancellationToken);
                    ctorsRunBefore.Add(nestedCtor).IgnoreReturnValue();
                    AddCtorsRecursively(runBefore, ctorsRunBefore, semanticModel, cancellationToken);
                }
            }
            else
            {
                var baseType = semanticModel.GetDeclaredSymbolSafe(ctor, cancellationToken)
                                            .ContainingType.BaseType;
                IMethodSymbol defaultCtor;
                if (TryGetDefault(baseType, out defaultCtor))
                {
                    foreach (var reference in defaultCtor.DeclaringSyntaxReferences)
                    {
                        ctorsRunBefore.Add(defaultCtor).IgnoreReturnValue();
                        var runBefore = (ConstructorDeclarationSyntax)reference.GetSyntax(cancellationToken);
                        AddCtorsRecursively(runBefore, ctorsRunBefore, semanticModel, cancellationToken);
                    }
                }
            }
        }
    }
}