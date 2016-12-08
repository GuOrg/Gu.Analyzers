namespace Gu.Analyzers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class AssignmentWalker : CSharpSyntaxWalker, IDisposable
    {
        private static readonly ConcurrentQueue<AssignmentWalker> Cache = new ConcurrentQueue<AssignmentWalker>();
        private readonly List<ExpressionSyntax> assignments = new List<ExpressionSyntax>();
        private ISymbol symbol;
        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;

        private AssignmentWalker()
        {
        }

        public IReadOnlyList<ExpressionSyntax> Assignments => this.assignments;

        public static AssignmentWalker Create(IPropertySymbol property, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return CreateCore(property, semanticModel, cancellationToken);
        }

        public static AssignmentWalker Create(IFieldSymbol field, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return CreateCore(field, semanticModel, cancellationToken);
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var left = this.semanticModel.SemanticModelFor(node)
                           .GetSymbolInfo(node.Left, this.cancellationToken)
                           .Symbol;
            if (ReferenceEquals(left, this.symbol))
            {
                this.assignments.Add(node.Right);
            }

            base.VisitAssignmentExpression(node);
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            if (node.Initializer != null &&
                ReferenceEquals(this.semanticModel.SemanticModelFor(node).GetDeclaredSymbol(node), this.symbol))
            {
                this.assignments.Add(node.Initializer.Value);
            }

            base.VisitVariableDeclarator(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (node.Initializer != null &&
                ReferenceEquals(this.semanticModel.SemanticModelFor(node).GetDeclaredSymbol(node), this.symbol))
            {
                this.assignments.Add(node.Initializer.Value);
            }

            base.VisitPropertyDeclaration(node);
        }

        public void Dispose()
        {
            this.assignments.Clear();
            this.symbol = null;
            this.semanticModel = null;
            this.cancellationToken = CancellationToken.None;
            Cache.Enqueue(this);
        }

        private static AssignmentWalker CreateCore(ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            AssignmentWalker walker;
            if (!Cache.TryDequeue(out walker))
            {
                walker = new AssignmentWalker();
            }

            walker.assignments.Clear();
            walker.symbol = symbol;
            walker.semanticModel = semanticModel;
            walker.cancellationToken = cancellationToken;
            foreach (var typeDeclaration in symbol.ContainingType.Declarations(cancellationToken))
            {
                walker.Visit(typeDeclaration);
            }

            return walker;
        }
    }
}