namespace Gu.Analyzers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class BlockSyntaxExt
    {
        internal static bool TryGetReturnExpression(this BlockSyntax body, out ExpressionSyntax returnValue)
        {
            using (var walker = ReturnWalker.Create(body))
            {
                if (walker.ReturnStatements.Count > 1)
                {
                    returnValue = null;
                    return false;
                }

                returnValue = walker.ReturnStatements.SingleOrDefault()
                               ?.Expression;
                return returnValue != null;
            }
        }

        internal static bool TryGetAssignment(this BlockSyntax body, ISymbol symbol, out AssignmentExpressionSyntax result)
        {
            result = null;
            using (var walker = AssignmentWalker.Create(body))
            {
                foreach (var assignment in walker.Assignments)
                {
                    if ((assignment.Right as IdentifierNameSyntax)?.Identifier.ValueText == symbol.Name)
                    {
                        result = assignment;
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool TryGetInvocation(this BlockSyntax body, ISymbol symbol, out InvocationExpressionSyntax result)
        {
            result = null;
            using (var walker = InvocationWalker.Create(body))
            {
                foreach (var invocation in walker.Invocations)
                {
                    if (invocation.ArgumentList?.Arguments.Any(a => (a.Expression as IdentifierNameSyntax)?.Identifier.ValueText == symbol.Name) == true)
                    {
                        result = invocation;
                        return true;
                    }
                }
            }

            return false;
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

        internal sealed class AssignmentWalker : CSharpSyntaxWalker, IDisposable
        {
            private static readonly ConcurrentQueue<AssignmentWalker> Cache = new ConcurrentQueue<AssignmentWalker>();
            private readonly List<AssignmentExpressionSyntax> assignments = new List<AssignmentExpressionSyntax>();

            private AssignmentWalker()
            {
            }

            public IReadOnlyList<AssignmentExpressionSyntax> Assignments => this.assignments;

            public static AssignmentWalker Create(BlockSyntax block)
            {
                AssignmentWalker walker;
                if (!Cache.TryDequeue(out walker))
                {
                    walker = new AssignmentWalker();
                }

                walker.assignments.Clear();
                walker.Visit(block);
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

        internal sealed class InvocationWalker : CSharpSyntaxWalker, IDisposable
        {
            private static readonly ConcurrentQueue<InvocationWalker> Cache = new ConcurrentQueue<InvocationWalker>();
            private readonly List<InvocationExpressionSyntax> invocations = new List<InvocationExpressionSyntax>();

            private InvocationWalker()
            {
            }

            public IReadOnlyList<InvocationExpressionSyntax> Invocations => this.invocations;

            public static InvocationWalker Create(BlockSyntax block)
            {
                InvocationWalker walker;
                if (!Cache.TryDequeue(out walker))
                {
                    walker = new InvocationWalker();
                }

                walker.invocations.Clear();
                walker.Visit(block);
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
}