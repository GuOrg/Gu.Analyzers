namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class ReturnValueWalker : CSharpSyntaxWalker
    {
        private static readonly Pool<ReturnValueWalker> Pool = new Pool<ReturnValueWalker>(
            () => new ReturnValueWalker(),
            x =>
            {
                x.values.Clear();
                x.checkedLocations.Clear();
                x.current = null;
                x.semanticModel = null;
                x.cancellationToken = CancellationToken.None;
            });

        private readonly List<ExpressionSyntax> values = new List<ExpressionSyntax>();
        private readonly HashSet<SyntaxNode> checkedLocations = new HashSet<SyntaxNode>();

        private bool isRecursive;
        private InvocationExpressionSyntax current;
        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;

        private ReturnValueWalker()
        {
        }

        public IReadOnlyList<ExpressionSyntax> Values => this.values;

        public override void Visit(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.AnonymousMethodExpression:
                    return;
                default:
                    base.Visit(node);
                    break;
            }
        }

        public override void VisitReturnStatement(ReturnStatementSyntax node)
        {
            this.AddReturnValue(node.Expression);
        }

        public override void VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
        {
            this.AddReturnValue(node.Expression);
        }

        internal static Pool<ReturnValueWalker>.Pooled Create(SyntaxNode node, bool recursive, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var pooled = Pool.GetOrCreate();
            pooled.Item.isRecursive = recursive;
            pooled.Item.semanticModel = semanticModel;
            pooled.Item.cancellationToken = cancellationToken;
            pooled.Item.Run(node);
            return pooled;
        }

        internal static bool TrygetSingle(BlockSyntax body, SemanticModel semanticModel, CancellationToken cancellationToken, out ExpressionSyntax returnValue)
        {
            using (var pooled = Create(body, false, semanticModel, cancellationToken))
            {
                if (pooled.Item.Values.Count > 1)
                {
                    returnValue = null;
                    return false;
                }

                returnValue = pooled.Item.values[0];
                return returnValue != null;
            }
        }

        private void AddReturnValue(ExpressionSyntax value)
        {
            this.values.Add(value);
            if (this.isRecursive && value is InvocationExpressionSyntax)
            {
                using (var pooled = Pool.GetOrCreate())
                {
                    pooled.Item.isRecursive = true;
                    pooled.Item.semanticModel = this.semanticModel;
                    pooled.Item.cancellationToken = this.cancellationToken;
                    pooled.Item.checkedLocations.UnionWith(this.checkedLocations);
                    pooled.Item.Run(value);
                    this.checkedLocations.UnionWith(pooled.Item.checkedLocations);
                    if (pooled.Item.values.Count != 0)
                    {
                        this.values.Remove(value);
                        foreach (var returnValue in pooled.Item.values)
                        {
                            this.values.Add(returnValue);
                        }
                    }
                }
            }

            var symbol = this.semanticModel.GetSymbolSafe(value, this.cancellationToken);
            if (symbol is ILocalSymbol ||
                symbol is IParameterSymbol)
            {
                using (var pooled = AssignedValueWalker.Create(value, this.semanticModel, this.cancellationToken))
                {
                    if (pooled.Item.Count != 0)
                    {
                        if (symbol is ILocalSymbol)
                        {
                            this.values.Remove(value);
                        }

                        foreach (var assignment in pooled.Item)
                        {
                            this.values.Add(assignment.Value);
                        }
                    }
                }
            }
        }

        private void Run(SyntaxNode node)
        {
            if (this.TryHandleInvocation(node as InvocationExpressionSyntax))
            {
                return;
            }

            this.checkedLocations.Add(node).IgnoreReturnValue();
            this.Visit(node);
        }

        private bool TryHandleInvocation(InvocationExpressionSyntax invocation)
        {
            if (invocation == null ||
                !this.checkedLocations.Add(invocation))
            {
                return false;
            }

            var method = this.semanticModel.GetSymbolSafe(invocation, this.cancellationToken);
            if (method == null ||
                method.DeclaringSyntaxReferences.Length == 0)
            {
                return true;
            }

            var old = this.current;
            this.current = invocation;
            foreach (var reference in method.DeclaringSyntaxReferences)
            {
                this.Visit(reference.GetSyntax(this.cancellationToken));
            }

            if (this.current != null)
            {
                for (var i = this.values.Count - 1; i >= 0; i--)
                {
                    ExpressionSyntax arg;
                    var symbol = this.semanticModel.GetSymbolSafe(this.values[i], this.cancellationToken);

                    if (this.isRecursive &&
                        SymbolComparer.Equals(symbol, method))
                    {
                        this.values.RemoveAt(i);
                        continue;
                    }

                    if (this.current.TryGetArgumentValue(symbol as IParameterSymbol, this.cancellationToken, out arg))
                    {
                        this.values[i] = arg;
                    }
                }

                for (var i = 0; i < this.values.Count; i++)
                {
                    for (var j = this.values.Count - 1; j > i; j--)
                    {
                        if (this.values[i] == this.values[j])
                        {
                            this.values.RemoveAt(j);
                        }
                    }
                }
            }

            this.current = old;
            return true;
        }
    }
}
