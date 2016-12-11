namespace Gu.Analyzers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class AssignmentWalker : CSharpSyntaxWalker, IDisposable
    {
        private static readonly ConcurrentQueue<AssignmentWalker> Cache = new ConcurrentQueue<AssignmentWalker>();
        private readonly List<AssignmentExpressionSyntax> assignments = new List<AssignmentExpressionSyntax>();

        private AssignmentWalker()
        {
        }

        public IReadOnlyList<AssignmentExpressionSyntax> Assignments => this.assignments;

        public static AssignmentWalker Create(SyntaxNode node)
        {
            AssignmentWalker walker;
            if (!Cache.TryDequeue(out walker))
            {
                walker = new AssignmentWalker();
            }

            walker.assignments.Clear();
            walker.Visit(node);
            return walker;
        }

        public static AssignmentWalker Create(IReadOnlyList<SyntaxNode> nodes)
        {
            AssignmentWalker walker;
            if (!Cache.TryDequeue(out walker))
            {
                walker = new AssignmentWalker();
            }

            walker.assignments.Clear();
            foreach (var node in nodes)
            {
                walker.Visit(node);
            }

            return walker;
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            this.assignments.Add(node);
            base.VisitAssignmentExpression(node);
        }

        public void Dispose()
        {
            this.assignments.Clear();
            Cache.Enqueue(this);
        }
    }
}