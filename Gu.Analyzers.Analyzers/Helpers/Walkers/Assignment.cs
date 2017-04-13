namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class Assignment : ExecutionWalker
    {
        private static readonly Pool<Assignment> Cache = new Pool<Assignment>(
            () => new Assignment(),
            x =>
                {
                    x.assignments.Clear();
                    x.Clear();
                    x.SemanticModel = null;
                    x.CancellationToken = CancellationToken.None;
                });

        private readonly List<AssignmentExpressionSyntax> assignments = new List<AssignmentExpressionSyntax>();

        private Assignment()
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

        internal static Pool<Assignment>.Pooled Create(SyntaxNode node, SearchMode searchMode, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var pooled = Cache.GetOrCreate();
            pooled.Item.SemanticModel = semanticModel;
            pooled.Item.CancellationToken = cancellationToken;
            pooled.Item.SearchMode = searchMode;
            pooled.Item.Visit(node);
            return pooled;
        }

        internal static bool FirstForSymbol(ISymbol symbol, SyntaxNode scope, SearchMode searchMode, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return FirstForSymbol(symbol, scope, searchMode, semanticModel, cancellationToken, out AssignmentExpressionSyntax _);
        }

        internal static bool FirstForSymbol(ISymbol symbol, SyntaxNode scope, SearchMode searchMode, SemanticModel semanticModel, CancellationToken cancellationToken, out AssignmentExpressionSyntax assignment)
        {
            assignment = null;
            if (symbol == null ||
                scope == null)
            {
                return false;
            }

            using (var pooledAssignments = Create(scope, searchMode, semanticModel, cancellationToken))
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

        internal static bool FirstWith(ISymbol symbol, SyntaxNode scope, SearchMode searchMode, SemanticModel semanticModel, CancellationToken cancellationToken, out AssignmentExpressionSyntax assignment)
        {
            assignment = null;
            if (symbol == null ||
                scope == null)
            {
                return false;
            }

            using (var pooledAssignments = Create(scope, searchMode, semanticModel, cancellationToken))
            {
                foreach (var candidate in pooledAssignments.Item.Assignments)
                {
                    var assignedSymbol = semanticModel.GetSymbolSafe(candidate.Right, cancellationToken);
                    if (SymbolComparer.Equals(symbol, assignedSymbol))
                    {
                        assignment = candidate;
                        return true;
                    }

                    if (candidate.Right is ObjectCreationExpressionSyntax objectCreation &&
                        objectCreation.ArgumentList != null &&
                        objectCreation.ArgumentList.Arguments.TryGetFirst(x => SymbolComparer.Equals(symbol, semanticModel.GetSymbolSafe(x.Expression, cancellationToken)), out ArgumentSyntax _))
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