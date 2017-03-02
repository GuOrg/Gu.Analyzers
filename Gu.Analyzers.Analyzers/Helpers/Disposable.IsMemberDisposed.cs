namespace Gu.Analyzers
{
    using System.Collections.Generic;
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
            foreach (var reference in disposeMethod.DeclaringSyntaxReferences)
            {
                using (var pooled = IdentifierNameWalker.Create(reference.GetSyntax(cancellationToken)))
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

        internal static bool TryGetDisposed(ExpressionStatementSyntax disposeCall, SemanticModel semanticModel, CancellationToken cancellationToken, out IdentifierNameSyntax value)
        {
            using (var pooled = GetDisposedPath(disposeCall, semanticModel, cancellationToken))
            {
                return pooled.Item.TryGetLast(out value);
            }
        }

        internal static Pool<List<IdentifierNameSyntax>>.Pooled GetDisposedPath(ExpressionStatementSyntax disposeCall, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var pooled = MemberPathWalker.Create(disposeCall))
            {
                if (pooled.Item.Count < 2 ||
                    semanticModel.GetSymbolSafe(pooled.Item[pooled.Item.Count - 1], cancellationToken) != KnownSymbol.IDisposable.Dispose)
                {
                    return ListPool<IdentifierNameSyntax>.Create();
                }

                var pooledList = ListPool<IdentifierNameSyntax>.Create();
                for (var i = 0; i < pooled.Item.Count - 1; i++)
                {
                    pooledList.Item.Add(pooled.Item[i]);
                }

                return pooledList;
            }
        }
    }
}