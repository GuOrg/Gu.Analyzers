namespace Gu.Analyzers
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class ReturnValueWalker : CSharpSyntaxWalker, IReadOnlyList<ExpressionSyntax>
    {
        private static readonly Pool<ReturnValueWalker> Pool = new Pool<ReturnValueWalker>(
            () => new ReturnValueWalker(),
            x =>
            {
                x.values.Clear();
                x.checkedLocations.Clear();
                x.isRecursive = false;
                x.awaits = false;
                x.semanticModel = null;
                x.cancellationToken = CancellationToken.None;
            });

        private readonly List<ExpressionSyntax> values = new List<ExpressionSyntax>();
        private readonly HashSet<SyntaxNode> checkedLocations = new HashSet<SyntaxNode>();

        private bool isRecursive;
        private bool awaits;
        private ExpressionSyntax current;
        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;

        private ReturnValueWalker()
        {
        }

        public int Count => this.values.Count;

        public ExpressionSyntax this[int index] => this.values[index];

        public IEnumerator<ExpressionSyntax> GetEnumerator() => this.values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public override void Visit(SyntaxNode node)
        {
            if (node == this.current)
            {
                base.Visit(node);
                return;
            }

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

        internal static bool TrygetSingle(BlockSyntax body, SemanticModel semanticModel, CancellationToken cancellationToken, out ExpressionSyntax returnValue)
        {
            using (var pooled = Create(body, false, semanticModel, cancellationToken))
            {
                if (pooled.Item.values.Count != 1)
                {
                    returnValue = null;
                    return false;
                }

                returnValue = pooled.Item.values[0];
                return returnValue != null;
            }
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

        private Pool<ReturnValueWalker>.Pooled GetRecursive(SyntaxNode node)
        {
            var pooled = Pool.GetOrCreate();
            pooled.Item.isRecursive = this.isRecursive;
            pooled.Item.awaits = this.awaits;
            pooled.Item.semanticModel = this.semanticModel;
            pooled.Item.cancellationToken = this.cancellationToken;
            pooled.Item.checkedLocations.UnionWith(this.checkedLocations);
            pooled.Item.Run(node);
            this.checkedLocations.UnionWith(pooled.Item.checkedLocations);
            return pooled;
        }

        private void AddReturnValue(ExpressionSyntax value)
        {
            var isTaskRun = false;
            if (this.awaits)
            {
                ExpressionSyntax awaited;
                isTaskRun = AsyncAwait.TryAwaitTaskRun(value as InvocationExpressionSyntax, this.semanticModel, this.cancellationToken, out awaited);
                if (isTaskRun ||
                    AsyncAwait.TryAwaitTaskFromResult(value as InvocationExpressionSyntax, this.semanticModel, this.cancellationToken, out awaited))
                {
                    value = awaited;
                }
            }

            this.values.Add(value);
            if ((this.isRecursive &&
                 value is InvocationExpressionSyntax) ||
                isTaskRun)
            {
                using (var pooled = this.GetRecursive(value))
                {
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
                            this.values.Add(assignment);
                        }
                    }
                }
            }
        }

        private void Run(SyntaxNode node)
        {
            if (!this.checkedLocations.Add(node))
            {
                return;
            }

            if (this.TryHandleInvocation(node as InvocationExpressionSyntax) ||
                this.TryHandleAwait(node as AwaitExpressionSyntax) ||
                this.TryHandlePropertyGet(node as ExpressionSyntax) ||
                this.TryHandleLambda(node as LambdaExpressionSyntax))
            {
                return;
            }

            this.Visit(node);
        }

        private bool TryHandleInvocation(InvocationExpressionSyntax invocation)
        {
            if (invocation == null)
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

                    if (invocation.TryGetArgumentValue(symbol as IParameterSymbol, this.cancellationToken, out arg))
                    {
                        this.values[i] = arg;
                    }
                }

                this.values.PurgeDuplicates();
            }

            this.current = old;
            return true;
        }

        private bool TryHandlePropertyGet(ExpressionSyntax propertyGet)
        {
            if (propertyGet == null)
            {
                return false;
            }

            var property = this.semanticModel.GetSymbolSafe(propertyGet, this.cancellationToken) as IPropertySymbol;
            var getter = property?.GetMethod;
            if (getter == null)
            {
                return false;
            }

            if (getter.DeclaringSyntaxReferences.Length == 0)
            {
                return true;
            }

            var old = this.current;
            this.current = propertyGet;
            foreach (var reference in getter.DeclaringSyntaxReferences)
            {
                this.Visit(reference.GetSyntax(this.cancellationToken));
            }

            if (this.current != null)
            {
                for (var i = this.values.Count - 1; i >= 0; i--)
                {
                    var symbol = this.semanticModel.GetSymbolSafe(this.values[i], this.cancellationToken);
                    if (this.isRecursive &&
                        SymbolComparer.Equals(symbol, property))
                    {
                        this.values.RemoveAt(i);
                    }
                }

                this.values.PurgeDuplicates();
            }

            this.current = old;
            return true;
        }

        private bool TryHandleAwait(AwaitExpressionSyntax @await)
        {
            if (@await == null)
            {
                return false;
            }

            InvocationExpressionSyntax invocation;
            if (AsyncAwait.TryGetAwaitedInvocation(@await, this.semanticModel, this.cancellationToken, out invocation))
            {
                this.awaits = true;
                var symbol = this.semanticModel.GetSymbolSafe(invocation, this.cancellationToken);
                if (symbol != null)
                {
                    if (symbol.DeclaringSyntaxReferences.Length == 0)
                    {
                        this.AddReturnValue(invocation);
                    }

                    return this.TryHandleInvocation(invocation);
                }

                return true;
            }

            return false;
        }

        private bool TryHandleLambda(LambdaExpressionSyntax lambda)
        {
            if (lambda == null)
            {
                return false;
            }

            this.current = lambda;
            var expressionBody = lambda.Body as ExpressionSyntax;
            if (expressionBody != null)
            {
                this.AddReturnValue(expressionBody);
            }
            else
            {
                this.Visit(lambda);
            }

            this.values.PurgeDuplicates();
            return true;
        }
    }
}
