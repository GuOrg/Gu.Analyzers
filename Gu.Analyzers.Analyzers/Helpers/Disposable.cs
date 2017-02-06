namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class Disposable
    {
        internal static bool IsMemberDisposed(ISymbol member, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!(member is IFieldSymbol || member is IPropertySymbol))
            {
                return false;
            }

            var containingType = member.ContainingType;
            IMethodSymbol disposeMethod;
            if (!IsAssignableTo(containingType) || !TryGetDisposeMethod(containingType, true, out disposeMethod))
            {
                return false;
            }

            return IsMemberDisposed(member, disposeMethod, semanticModel, cancellationToken);
        }

        internal static bool IsMemberDisposed(ISymbol member, IMethodSymbol disposeMethod, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            foreach (var declaration in disposeMethod.Declarations(cancellationToken))
            {
                using (var pooled = IdentifierNameWalker.Create(declaration))
                {
                    foreach (var identifier in pooled.Item.IdentifierNames)
                    {
                        var memberAccess = identifier.Parent as MemberAccessExpressionSyntax;
                        if (memberAccess?.Expression is BaseExpressionSyntax)
                        {
                            var baseMethod = semanticModel.GetSymbolSafe(identifier, cancellationToken) as IMethodSymbol;
                            if (baseMethod?.Name == "Dispose")
                            {
                                if (IsMemberDisposed(member, baseMethod, semanticModel, cancellationToken))
                                {
                                    return true;
                                }
                            }
                        }

                        if (identifier.Identifier.ValueText != member.Name)
                        {
                            continue;
                        }

                        var symbol = semanticModel.GetSymbolSafe(identifier, cancellationToken);
                        if (member.Equals(symbol) || (member as IPropertySymbol)?.OverriddenProperty?.Equals(symbol) == true)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal static bool IsAssignedWithCreatedAndNotCachedOrInjected(IFieldSymbol field, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (field == null ||
                !IsPotentiallyAssignableTo(field.Type))
            {
                return false;
            }

            using (var sources = VauleWithSource.GetRecursiveSources(field, semanticModel, cancellationToken))
            {
                return IsPotentiallyCreated(sources, semanticModel, cancellationToken) &&
                       !IsPotentiallyCachedOrInjected(sources, semanticModel, cancellationToken);
            }
        }

        internal static bool IsAssignedWithCreatedAndNotCachedOrInjected(IPropertySymbol property, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (property == null ||
                 !IsPotentiallyAssignableTo(property.Type))
            {
                return false;
            }

            using (var sources = VauleWithSource.GetRecursiveSources(property, semanticModel, cancellationToken))
            {
                return IsPotentiallyCreated(sources, semanticModel, cancellationToken) &&
                       !IsPotentiallyCachedOrInjected(sources, semanticModel, cancellationToken);
            }
        }

        /// <summary>
        /// Check if any path returns a created IDisposable
        /// </summary>
        internal static bool IsPotentiallyCreated(ExpressionSyntax disposable, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (disposable == null ||
                disposable.IsMissing ||
                !IsPotentiallyAssignableTo(semanticModel.GetTypeInfoSafe(disposable, cancellationToken).Type))
            {
                return false;
            }

            using (var sources = VauleWithSource.GetRecursiveSources(disposable, semanticModel, cancellationToken))
            {
                return IsPotentiallyCreated(sources, semanticModel, cancellationToken);
            }
        }

        internal static bool IsPotentiallyCreatedAndNotCachedInMember(ExpressionSyntax disposable, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (disposable == null ||
                disposable.IsMissing ||
                !IsPotentiallyAssignableTo(semanticModel.GetTypeInfoSafe(disposable, cancellationToken).Type))
            {
                return false;
            }

            using (var sources = VauleWithSource.GetRecursiveSources(disposable, semanticModel, cancellationToken))
            {
                return IsPotentiallyCreated(sources, semanticModel, cancellationToken) &&
                       !IsPotentiallyCachedOrInjected(sources, semanticModel, cancellationToken) &&
                       !IsCachedInMember(sources, semanticModel, cancellationToken);
            }
        }

        internal static bool IsAssignedWithCreatedAndInjected(IFieldSymbol field, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (field == null ||
                !IsPotentiallyAssignableTo(field.Type))
            {
                return false;
            }

            using (var sources = VauleWithSource.GetRecursiveSources(field, semanticModel, cancellationToken))
            {
                return IsPotentiallyCreated(sources, semanticModel, cancellationToken) &&
                       IsPotentiallyCachedOrInjected(sources, semanticModel, cancellationToken);
            }
        }

        internal static bool IsAssignedWithCreatedAndInjected(IPropertySymbol property, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (property == null ||
                !IsPotentiallyAssignableTo(property.Type))
            {
                return false;
            }

            using (var sources = VauleWithSource.GetRecursiveSources(property, semanticModel, cancellationToken))
            {
                return IsPotentiallyCreated(sources, semanticModel, cancellationToken) &&
                       IsPotentiallyCachedOrInjected(sources, semanticModel, cancellationToken);
            }
        }

        /// <summary>
        /// Check if any path returns a created IDisposable
        /// </summary>
        internal static bool IsPotentiallyCachedOrInjected(ExpressionSyntax disposable, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (disposable == null ||
                disposable.IsMissing ||
                !IsPotentiallyAssignableTo(semanticModel.GetTypeInfoSafe(disposable, cancellationToken).Type))
            {
                return false;
            }

            using (var sources = VauleWithSource.GetRecursiveSources(disposable, semanticModel, cancellationToken))
            {
                return IsPotentiallyCachedOrInjected(sources, semanticModel, cancellationToken);
            }
        }

        internal static bool IsPotentiallyAssignableTo(ITypeSymbol type)
        {
            if (type == null)
            {
                return false;
            }

            if (type.IsValueType &&
                !IsAssignableTo(type))
            {
                return false;
            }

            if (type.IsSealed &&
                !IsAssignableTo(type))
            {
                return false;
            }

            return true;
        }

        internal static bool IsAssignableTo(ITypeSymbol type)
        {
            if (type == null)
            {
                return false;
            }

            // https://blogs.msdn.microsoft.com/pfxteam/2012/03/25/do-i-need-to-dispose-of-tasks/
            if (type == KnownSymbol.Task)
            {
                return false;
            }

            ITypeSymbol _;
            return type == KnownSymbol.IDisposable ||
                   type.AllInterfaces.TryGetSingle(x => x == KnownSymbol.IDisposable, out _);
        }

        internal static bool TryGetDisposeMethod(ITypeSymbol type, bool recursive, out IMethodSymbol disposeMethod)
        {
            disposeMethod = null;
            if (type == null)
            {
                return false;
            }

            var disposers = type.GetMembers("Dispose");
            if (disposers.Length == 0)
            {
                var baseType = type.BaseType;
                if (recursive && IsAssignableTo(baseType))
                {
                    return TryGetDisposeMethod(baseType, true, out disposeMethod);
                }

                return false;
            }

            if (disposers.Length == 1)
            {
                disposeMethod = disposers[0] as IMethodSymbol;
                if (disposeMethod == null)
                {
                    return false;
                }

                return (disposeMethod.Parameters.Length == 0 &&
                        disposeMethod.DeclaredAccessibility == Accessibility.Public) ||
                       (disposeMethod.Parameters.Length == 1 &&
                        disposeMethod.Parameters[0].Type == KnownSymbol.Boolean);
            }

            if (disposers.Length == 2)
            {
                ISymbol temp;
                if (disposers.TryGetSingle(x => (x as IMethodSymbol)?.Parameters.Length == 1, out temp))
                {
                    disposeMethod = temp as IMethodSymbol;
                    return disposeMethod != null &&
                           disposeMethod.Parameters[0].Type == KnownSymbol.Boolean;
                }
            }

            return false;
        }

        internal static bool BaseTypeHasVirtualDisposeMethod(ITypeSymbol type)
        {
            var baseType = type.BaseType;
            while (baseType != null)
            {
                foreach (var member in baseType.GetMembers("Dispose"))
                {
                    var method = member as IMethodSymbol;
                    if (method == null)
                    {
                        continue;
                    }

                    if (member.DeclaredAccessibility == Accessibility.Protected &&
                        member.IsVirtual &&
                        method.Parameters.Length == 1 &&
                        method.Parameters[0].Type == KnownSymbol.Boolean)
                    {
                        return true;
                    }
                }

                baseType = baseType.BaseType;
            }

            return false;
        }

        private static bool IsPotentiallyCreated(Pool<List<VauleWithSource>>.Pooled sources, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            foreach (var vauleWithSource in sources.Item)
            {
                switch (vauleWithSource.Source)
                {
                    case ValueSource.Created:
                    case ValueSource.PotentiallyCreated:
                        if (IsAssignableTo(semanticModel.GetTypeInfoSafe(vauleWithSource.Value, cancellationToken).Type))
                        {
                            return true;
                        }

                        break;
                    case ValueSource.External:
                        var type = semanticModel.GetTypeInfoSafe(vauleWithSource.Value, cancellationToken).Type;
                        if (IsAssignableTo(type))
                        {
                            var symbol = semanticModel.GetSymbolSafe(vauleWithSource.Value, cancellationToken);
                            var property = symbol as IPropertySymbol;
                            if (property != null &&
                                property == KnownSymbol.PasswordBox.SecurePassword)
                            {
                                return true;
                            }

                            var method = symbol as IMethodSymbol;
                            if (method != null)
                            {
                                if (method.ContainingType == KnownSymbol.Enumerable ||
                                    method.ContainingType.Is(KnownSymbol.IDictionary) ||
                                    method == KnownSymbol.IEnumerable.GetEnumerator)
                                {
                                    continue;
                                }

                                return true;
                            }
                        }

                        break;
                }
            }

            return false;
        }

        private static bool IsPotentiallyCachedOrInjected(Pool<List<VauleWithSource>>.Pooled sources, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            foreach (var vauleWithSource in sources.Item)
            {
                switch (vauleWithSource.Source)
                {
                    case ValueSource.Injected:
                    case ValueSource.PotentiallyInjected:
                    case ValueSource.Cached:
                        if (IsAssignableTo(semanticModel.GetTypeInfoSafe(vauleWithSource.Value, cancellationToken).Type))
                        {
                            return true;
                        }

                        break;
                }
            }

            return false;
        }

        private static bool IsCachedInMember(Pool<List<VauleWithSource>>.Pooled sources, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            foreach (var vauleWithSource in sources.Item)
            {
                switch (vauleWithSource.Source)
                {
                    case ValueSource.Member:
                        if (IsAssignableTo(semanticModel.GetTypeInfoSafe(vauleWithSource.Value, cancellationToken).Type))
                        {
                            return true;
                        }

                        break;
                }
            }

            return false;
        }
    }
}