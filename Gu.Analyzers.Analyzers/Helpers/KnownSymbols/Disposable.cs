namespace Gu.Analyzers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class Disposable
    {
        internal static bool IsCreation(ExpressionSyntax disposable, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var symbol = semanticModel.SemanticModelFor(disposable)
                          .GetSymbolInfo(disposable, cancellationToken)
                          .Symbol;

            if (disposable is ObjectCreationExpressionSyntax)
            {
                return IsAssignableTo(symbol.ContainingType);
            }

            if (symbol is IFieldSymbol)
            {
                return false;
            }

            var methodSymbol = symbol as IMethodSymbol;
            if (methodSymbol != null)
            {
                MethodDeclarationSyntax methodDeclaration;
                if (methodSymbol.TryGetSingleDeclaration(cancellationToken, out methodDeclaration))
                {
                    ExpressionSyntax returnValue;
                    if (methodDeclaration.TryGetReturnExpression(out returnValue))
                    {
                        return IsCreation(returnValue, semanticModel, cancellationToken);
                    }
                }

                return IsAssignableTo(methodSymbol.ReturnType);
            }

            var property = symbol as IPropertySymbol;
            if (property != null)
            {
                if (property == KnownSymbol.PasswordBox.SecurePassword)
                {
                    return true;
                }

                PropertyDeclarationSyntax propertyDeclaration;
                if (property.TryGetSingleDeclaration(cancellationToken, out propertyDeclaration))
                {
                    if (propertyDeclaration.ExpressionBody != null)
                    {
                        return IsCreation(propertyDeclaration.ExpressionBody.Expression, semanticModel, cancellationToken);
                    }

                    AccessorDeclarationSyntax getter;
                    if (propertyDeclaration.TryGetGetAccessorDeclaration(out getter))
                    {
                        ExpressionSyntax returnValue;
                        if (getter.Body.TryGetReturnExpression(out returnValue))
                        {
                            return IsCreation(returnValue, semanticModel, cancellationToken);
                        }
                    }
                }

                return false;
            }

            var local = symbol as ILocalSymbol;
            if (local != null)
            {
                VariableDeclaratorSyntax variable;
                if (local.TryGetSingleDeclaration(cancellationToken, out variable))
                {
                    return IsCreation(variable.Initializer.Value, semanticModel, cancellationToken);
                }
            }

            return false;
        }

        internal static bool IsAssignableTo(ITypeSymbol type)
        {
            if (type == null)
            {
                return false;
            }

            ITypeSymbol _;
            return type == KnownSymbol.IDisposable ||
                   type.AllInterfaces.TryGetSingle(x => x == KnownSymbol.IDisposable, out _);
        }

        internal static DisposeWalker CreateDisposeWalker(BlockSyntax block, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return DisposeWalker.Create(block, semanticModel, cancellationToken);
        }

        internal sealed class DisposeWalker : CSharpSyntaxWalker, IDisposable
        {
            private static readonly ConcurrentQueue<DisposeWalker> Cache = new ConcurrentQueue<DisposeWalker>();
            private readonly List<InvocationExpressionSyntax> disposeCalls = new List<InvocationExpressionSyntax>();

            private SemanticModel semanticModel;
            private CancellationToken cancellationToken;

            private DisposeWalker()
            {
            }

            public IReadOnlyList<InvocationExpressionSyntax> DisposeCalls => this.disposeCalls;

            public static DisposeWalker Create(BlockSyntax block, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                DisposeWalker walker;
                if (!Cache.TryDequeue(out walker))
                {
                    walker = new DisposeWalker();
                }

                walker.disposeCalls.Clear();
                walker.semanticModel = semanticModel;
                walker.cancellationToken = cancellationToken;
                walker.Visit(block);
                return walker;
            }

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                var symbol = this.semanticModel.SemanticModelFor(node).GetSymbolInfo(node, this.cancellationToken).Symbol;
                if (symbol.Name == KnownSymbol.IDisposable.Dispose.Name)
                {
                    this.disposeCalls.Add(node);
                }

                base.VisitInvocationExpression(node);
            }

            public void Dispose()
            {
                this.disposeCalls.Clear();
                this.semanticModel = null;
                this.cancellationToken = CancellationToken.None;
                Cache.Enqueue(this);
            }
        }
    }
}