namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class Assigns : ExecutionWalker
    {
        private static readonly Pool<Assigns> Cache = new Pool<Assigns>(
            () => new Assigns(),
            x =>
                {
                    x.assignments.Clear();
                    x.Clear();
                    x.IsRecursive = false;
                    x.semanticModel = null;
                    x.cancellationToken = CancellationToken.None;
                });

        private readonly List<AssignmentExpressionSyntax> assignments = new List<AssignmentExpressionSyntax>();

        private Assigns()
        {
        }

        /// <summary>
        /// Gets a list with all <see cref="AssignmentExpressionSyntax"/> in the scope.
        /// </summary>
        public IReadOnlyList<AssignmentExpressionSyntax> Assignments => this.assignments;

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            base.VisitAssignmentExpression(node);
            this.assignments.Add(node);
        }

        internal static Pool<Assigns>.Pooled Create(SyntaxNode node, bool recursive, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var pooled = Cache.GetOrCreate();
            pooled.Item.semanticModel = semanticModel;
            pooled.Item.cancellationToken = cancellationToken;
            pooled.Item.IsRecursive = recursive;
            pooled.Item.Visit(node);
            return pooled;
        }

        internal static bool FirstSymbol(ISymbol symbol, SyntaxNode scope, bool recursive, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            AssignmentExpressionSyntax temp;
            return FirstSymbol(symbol, scope, recursive, semanticModel, cancellationToken, out temp);
        }

        internal static bool FirstSymbol(ISymbol symbol, SyntaxNode scope, bool recursive, SemanticModel semanticModel, CancellationToken cancellationToken, out AssignmentExpressionSyntax assignment)
        {
            assignment = null;
            if (symbol == null ||
                scope == null)
            {
                return false;
            }

            using (var pooledAssignments = Create(scope, recursive, semanticModel, cancellationToken))
            {
                foreach (var candidate in pooledAssignments.Item.Assignments)
                {
                    var assignedSymbol = semanticModel.GetSymbolSafe(candidate.Left, cancellationToken);
                    if (SymbolComparer.Equals(symbol, assignedSymbol))
                    {
                        assignment = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool FirstWith(ISymbol symbol, SyntaxNode scope, bool recursive, SemanticModel semanticModel, CancellationToken cancellationToken, out AssignmentExpressionSyntax assignment)
        {
            assignment = null;
            if (symbol == null ||
                scope == null)
            {
                return false;
            }

            using (var pooledAssignments = Create(scope, recursive, semanticModel, cancellationToken))
            {
                foreach (var candidate in pooledAssignments.Item.Assignments)
                {
                    var assignedSymbol = semanticModel.GetSymbolSafe(candidate.Right, cancellationToken);
                    if (SymbolComparer.Equals(symbol, assignedSymbol))
                    {
                        assignment = candidate;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}