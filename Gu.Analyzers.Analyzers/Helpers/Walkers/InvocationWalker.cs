namespace Gu.Analyzers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class InvocationWalker : CSharpSyntaxWalker, IDisposable
    {
        private static readonly ConcurrentQueue<InvocationWalker> Cache = new ConcurrentQueue<InvocationWalker>();
        private readonly List<InvocationExpressionSyntax> invocations = new List<InvocationExpressionSyntax>();

        private InvocationWalker()
        {
        }

        public IReadOnlyList<InvocationExpressionSyntax> Invocations => this.invocations;

        public static InvocationWalker Create(SyntaxNode node)
        {
            InvocationWalker walker;
            if (!Cache.TryDequeue(out walker))
            {
                walker = new InvocationWalker();
            }

            walker.invocations.Clear();
            if (node != null)
            {
                walker.Visit(node);
            }

            return walker;
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            this.invocations.Add(node);
            base.VisitInvocationExpression(node);
        }

        public void Dispose()
        {
            this.invocations.Clear();
            Cache.Enqueue(this);
        }
    }
}