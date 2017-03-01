namespace Gu.Analyzers
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
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
            return DisposeWalker.TryGetDisposeInvocation(disposeCall, semanticModel, cancellationToken, out value);
        }

        private sealed class DisposeWalker : CSharpSyntaxWalker, IReadOnlyList<IdentifierNameSyntax>
        {
            private static readonly Pool<DisposeWalker> Pool = new Pool<DisposeWalker>(
                () => new DisposeWalker(),
                x =>
                    {
                        x.invocations.Clear();
                        x.names.Clear();
                        x.Success = false;
                        x.semanticModel = null;
                        x.cancellationToken = CancellationToken.None;
                    });

            private readonly List<InvocationExpressionSyntax> invocations = new List<InvocationExpressionSyntax>();
            private readonly List<IdentifierNameSyntax> names = new List<IdentifierNameSyntax>();
            private SemanticModel semanticModel;
            private CancellationToken cancellationToken;

            private DisposeWalker()
            {
            }

            public int Count => this.names.Count;

            public bool Success { get; private set; }

            public IdentifierNameSyntax this[int index] => this.names[index];

            public IEnumerator<IdentifierNameSyntax> GetEnumerator()
            {
                return this.names.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)this.names).GetEnumerator();
            }

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                if (this.semanticModel.GetSymbolSafe(node, this.cancellationToken) == KnownSymbol.IDisposable.Dispose)
                {
                    this.invocations.Add(node);
                }

                base.VisitInvocationExpression(node);
            }

            public override void VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.AsExpression:
                        this.Visit(node.Left);
                        return;
                }

                base.VisitBinaryExpression(node);
            }

            public override void VisitIdentifierName(IdentifierNameSyntax node)
            {
                this.names.Add(node);
                base.VisitIdentifierName(node);
            }

            internal static bool TryGetDisposeInvocation(ExpressionStatementSyntax disposeCall, SemanticModel semanticModel, CancellationToken cancellationToken, out IdentifierNameSyntax value)
            {
                using (var pooled = Create(disposeCall, semanticModel, cancellationToken))
                {
                    if (pooled.Item.Success)
                    {
                        value = pooled.Item.names[pooled.Item.names.Count - 1];
                        return true;
                    }

                    value = null;
                    return false;
                }
            }

            internal static Pool<DisposeWalker>.Pooled Create(ExpressionStatementSyntax node, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                var pooled = Pool.GetOrCreate();
                pooled.Item.semanticModel = semanticModel;
                pooled.Item.cancellationToken = cancellationToken;
                if (node != null)
                {
                    pooled.Item.Visit(node);

                    if (pooled.Item.names.Count > 1 &&
                        semanticModel.GetSymbolSafe(pooled.Item.names[pooled.Item.names.Count - 1], cancellationToken) == KnownSymbol.IDisposable.Dispose)
                    {
                        pooled.Item.names.RemoveAt(pooled.Item.names.Count - 1);
                        pooled.Item.Success = IsPotentiallyAssignableTo(pooled.Item.names[pooled.Item.names.Count - 1], semanticModel, cancellationToken);
                    }
                }

                return pooled;
            }
        }
    }
}