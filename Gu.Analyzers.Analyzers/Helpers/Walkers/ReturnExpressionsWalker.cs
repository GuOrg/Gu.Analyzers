namespace Gu.Analyzers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class ReturnExpressionsWalker : CSharpSyntaxWalker, IDisposable
    {
        private static readonly ConcurrentQueue<ReturnExpressionsWalker> Cache = new ConcurrentQueue<ReturnExpressionsWalker>();
        private readonly List<ExpressionSyntax> returnValues = new List<ExpressionSyntax>();

        private ReturnExpressionsWalker()
        {
        }

        public IReadOnlyList<ExpressionSyntax> ReturnValues => this.returnValues;

        public static ReturnExpressionsWalker Create(SyntaxNode node)
        {
            ReturnExpressionsWalker walker;
            if (!Cache.TryDequeue(out walker))
            {
                walker = new ReturnExpressionsWalker();
            }

            walker.returnValues.Clear();
            walker.Visit(node);
            return walker;
        }

        public override void VisitReturnStatement(ReturnStatementSyntax node)
        {
            this.returnValues.Add(node.Expression);
            base.VisitReturnStatement(node);
        }

        public override void VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
        {
            this.returnValues.Add(node.Expression);
            base.VisitArrowExpressionClause(node);
        }

        public void Dispose()
        {
            this.returnValues.Clear();
            Cache.Enqueue(this);
        }
    }
}
