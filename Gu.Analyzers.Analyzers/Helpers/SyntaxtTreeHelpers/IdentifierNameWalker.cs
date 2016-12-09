namespace Gu.Analyzers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class IdentifierNameWalker : CSharpSyntaxWalker, IDisposable
    {
        private static readonly ConcurrentQueue<IdentifierNameWalker> Cache = new ConcurrentQueue<IdentifierNameWalker>();
        private readonly List<IdentifierNameSyntax> identifierNames = new List<IdentifierNameSyntax>();

        private IdentifierNameWalker()
        {
        }

        public IReadOnlyList<IdentifierNameSyntax> IdentifierNames => this.identifierNames;

        public static IdentifierNameWalker Create(SyntaxNode node)
        {
            IdentifierNameWalker walker;
            if (!Cache.TryDequeue(out walker))
            {
                walker = new IdentifierNameWalker();
            }

            walker.identifierNames.Clear();
            walker.Visit(node);
            return walker;
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            this.identifierNames.Add(node);
            base.VisitIdentifierName(node);
        }

        public void Dispose()
        {
            this.identifierNames.Clear();
            Cache.Enqueue(this);
        }
    }
}