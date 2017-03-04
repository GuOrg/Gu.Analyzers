namespace Gu.Analyzers
{
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
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
            if (member == null ||
                disposeMethod == null)
            {
                return false;
            }

            foreach (var reference in disposeMethod.DeclaringSyntaxReferences)
            {
                var node = reference.GetSyntax(cancellationToken);
                using (var pooled = InvocationWalker.Create(node))
                {
                    foreach (var invocation in pooled.Item)
                    {
                        var method = semanticModel.GetSymbolSafe(invocation, cancellationToken) as IMethodSymbol;
                        if (method == null ||
                            method.Parameters.Length != 0 ||
                            method != KnownSymbol.IDisposable.Dispose)
                        {
                            continue;
                        }

                        ExpressionSyntax disposed;
                        if (TryGetDisposedRootMember(invocation, semanticModel, cancellationToken, out disposed))
                        {
                            if (SymbolComparer.Equals(member, semanticModel.GetSymbolSafe(disposed, cancellationToken)))
                            {
                                return true;
                            }
                        }
                    }
                }

                using (var pooled = IdentifierNameWalker.Create(node))
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

        internal static bool TryGetDisposedRootMember(InvocationExpressionSyntax disposeCall, SemanticModel semanticModel, CancellationToken cancellationToken, out ExpressionSyntax disposedMember)
        {
            if (MemberPath.TryFindRootMember(disposeCall, out disposedMember))
            {
                var property = semanticModel.GetSymbolSafe(disposedMember, cancellationToken) as IPropertySymbol;
                if (property == null ||
                    property.IsAutoProperty(cancellationToken))
                {
                    return true;
                }

                if (property.GetMethod == null)
                {
                    return false;
                }

                foreach (var reference in property.GetMethod.DeclaringSyntaxReferences)
                {
                    var node = reference.GetSyntax(cancellationToken);
                    using (var pooled = ReturnValueWalker.Create(node, false, semanticModel, cancellationToken))
                    {
                        if (pooled.Item.Count == 0)
                        {
                            return false;
                        }

                        return MemberPath.TryFindRootMember(pooled.Item[0], out disposedMember);
                    }
                }
            }

            return false;
        }
    }
}