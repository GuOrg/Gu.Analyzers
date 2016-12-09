namespace Gu.Analyzers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class SimpleMemberAccessesWalker : CSharpSyntaxWalker, IDisposable
    {
        private static readonly ConcurrentQueue<SimpleMemberAccessesWalker> Cache = new ConcurrentQueue<SimpleMemberAccessesWalker>();
        private readonly List<MemberAccessExpressionSyntax> simpleMemberAccesses = new List<MemberAccessExpressionSyntax>();

        private SimpleMemberAccessesWalker()
        {
        }

        public IReadOnlyList<MemberAccessExpressionSyntax> SimpleMemberAccesses => this.simpleMemberAccesses;

        public static SimpleMemberAccessesWalker Create(SyntaxNode node)
        {
            SimpleMemberAccessesWalker walker;
            if (!Cache.TryDequeue(out walker))
            {
                walker = new SimpleMemberAccessesWalker();
            }

            walker.simpleMemberAccesses.Clear();
            walker.Visit(node);
            return walker;
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                this.simpleMemberAccesses.Add(node);
            }

            base.VisitMemberAccessExpression(node);
        }

        public void Dispose()
        {
            this.simpleMemberAccesses.Clear();
            Cache.Enqueue(this);
        }
    }
}