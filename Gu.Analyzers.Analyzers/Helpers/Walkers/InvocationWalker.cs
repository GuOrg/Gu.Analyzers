namespace Gu.Analyzers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [Obsolete("Rewrite as Execution walker and add recursive.")]
    internal sealed class InvocationWalker : CSharpSyntaxWalker, IReadOnlyList<InvocationExpressionSyntax>
    {
        private static readonly Pool<InvocationWalker> Pool = new Pool<InvocationWalker>(
            () => new InvocationWalker(),
            x => x.invocations.Clear());

        private readonly List<InvocationExpressionSyntax> invocations = new List<InvocationExpressionSyntax>();

        private InvocationWalker()
        {
        }

        public IReadOnlyList<InvocationExpressionSyntax> Invocations => this.invocations;

        public int Count => this.invocations.Count;

        public InvocationExpressionSyntax this[int index] => this.invocations[index];

        public IEnumerator<InvocationExpressionSyntax> GetEnumerator() => this.invocations.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.invocations).GetEnumerator();

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            this.invocations.Add(node);
            base.VisitInvocationExpression(node);
        }

        internal static Pool<InvocationWalker>.Pooled Create(SyntaxNode node)
        {
            var pooled = Pool.GetOrCreate();
            if (node != null)
            {
                pooled.Item.Visit(node);
            }

            return pooled;
        }
    }
}