namespace Gu.Analyzers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class BlockSyntaxExt
    {
        internal static bool TryGetReturnExpression(this BlockSyntax body, out ExpressionSyntax result)
        {
            using (var walker = ReturnWalker.Create(body))
            {
                if (walker.ReturnStatements.Count > 1)
                {
                    result = null;
                    return false;
                }

                result = walker.ReturnStatements.SingleOrDefault()
                               ?.Expression;
                return result != null;
            }
        }

        internal sealed class ReturnWalker : CSharpSyntaxWalker, IDisposable
        {
            private static readonly ConcurrentQueue<ReturnWalker> Cache = new ConcurrentQueue<ReturnWalker>();
            private readonly List<ReturnStatementSyntax> returnStatements = new List<ReturnStatementSyntax>();

            private ReturnWalker()
            {
            }

            public IReadOnlyList<ReturnStatementSyntax> ReturnStatements => this.returnStatements;

            public static ReturnWalker Create(BlockSyntax block)
            {
                ReturnWalker walker;
                if (!Cache.TryDequeue(out walker))
                {
                    walker = new ReturnWalker();
                }

                walker.returnStatements.Clear();
                walker.Visit(block);
                return walker;
            }

            public override void VisitReturnStatement(ReturnStatementSyntax node)
            {
                this.returnStatements.Add(node);
                base.VisitReturnStatement(node);
            }

            public void Dispose()
            {
                this.returnStatements.Clear();
                Cache.Enqueue(this);
            }
        }
    }
}