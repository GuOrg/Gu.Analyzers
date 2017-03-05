namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class Assigns : CSharpSyntaxWalker
    {
        private static readonly Pool<Assigns> Cache = new Pool<Assigns>(
            () => new Assigns(),
            x =>
                {
                    x.assignments.Clear();
                    x.visited.Clear();
                    x.recursive = false;
                    x.semanticModel = null;
                    x.cancellationToken = CancellationToken.None;
                });

        private readonly List<AssignmentExpressionSyntax> assignments = new List<AssignmentExpressionSyntax>();
        private readonly HashSet<SyntaxNode> visited = new HashSet<SyntaxNode>();

        private bool recursive;
        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;

        private Assigns()
        {
        }

        /// <summary>
        /// Gets a list with all <see cref="AssignmentExpressionSyntax"/> in the scope.
        /// </summary>
        public IReadOnlyList<AssignmentExpressionSyntax> Assignments => this.assignments;

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            this.assignments.Add(node);
            base.VisitAssignmentExpression(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            base.VisitInvocationExpression(node);
            this.VisitChained(node);
        }

        public override void VisitConstructorInitializer(ConstructorInitializerSyntax node)
        {
            base.VisitConstructorInitializer(node);
            this.VisitChained(node);
        }

        internal static Pool<Assigns>.Pooled Create(SyntaxNode node, bool recursive, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var pooled = Cache.GetOrCreate();
            pooled.Item.semanticModel = semanticModel;
            pooled.Item.cancellationToken = cancellationToken;
            pooled.Item.recursive = recursive;
            pooled.Item.Visit(node);
            return pooled;
        }

        internal static bool Symbol(ISymbol symbol, SyntaxNode scope, bool recursive, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            AssignmentExpressionSyntax temp;
            return Symbol(symbol, scope, recursive, semanticModel, cancellationToken, out temp);
        }

        internal static bool Symbol(ISymbol symbol, SyntaxNode scope, bool recursive, SemanticModel semanticModel, CancellationToken cancellationToken, out AssignmentExpressionSyntax assignment)
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

        internal static bool With(ISymbol symbol, SyntaxNode scope, bool recursive, SemanticModel semanticModel, CancellationToken cancellationToken, out AssignmentExpressionSyntax assignment)
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

        private void VisitChained(SyntaxNode node)
        {
            if (this.recursive &&
                this.visited.Add(node))
            {
                var method = this.semanticModel.GetSymbolSafe(node, this.cancellationToken);
                foreach (var reference in method.DeclaringSyntaxReferences)
                {
                    this.Visit(reference.GetSyntax(this.cancellationToken));
                }
            }
        }
    }
}