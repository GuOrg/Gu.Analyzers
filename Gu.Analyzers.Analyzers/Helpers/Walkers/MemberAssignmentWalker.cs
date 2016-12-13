namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class MemberAssignmentWalker : CSharpSyntaxWalker
    {
        private static readonly Pool<MemberAssignmentWalker> Pool = new Pool<MemberAssignmentWalker>(
            () => new MemberAssignmentWalker(),
            x =>
            {
                x.assignments.Clear();
                x.symbol = null;
                x.semanticModel = null;
                x.cancellationToken = CancellationToken.None;
            });

        private readonly List<ExpressionSyntax> assignments = new List<ExpressionSyntax>();
        private ISymbol symbol;
        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;

        private MemberAssignmentWalker()
        {
        }

        public IReadOnlyList<ExpressionSyntax> Assignments => this.assignments;

        public static Pool<MemberAssignmentWalker>.Pooled Create(IPropertySymbol property, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return CreateCore(property, semanticModel, cancellationToken);
        }

        public static Pool<MemberAssignmentWalker>.Pooled Create(IFieldSymbol field, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return CreateCore(field, semanticModel, cancellationToken);
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var left = this.semanticModel.GetSymbolSafe(node.Left, this.cancellationToken);
            if (ReferenceEquals(left, this.symbol))
            {
                this.assignments.Add(node.Right);
            }

            base.VisitAssignmentExpression(node);
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            if (node.Initializer != null &&
                ReferenceEquals(this.semanticModel.GetDeclaredSymbolSafe(node, this.cancellationToken), this.symbol))
            {
                this.assignments.Add(node.Initializer.Value);
            }

            base.VisitVariableDeclarator(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (node.Initializer != null &&
                ReferenceEquals(this.semanticModel.GetDeclaredSymbolSafe(node, this.cancellationToken), this.symbol))
            {
                this.assignments.Add(node.Initializer.Value);
            }

            base.VisitPropertyDeclaration(node);
        }

        private static Pool<MemberAssignmentWalker>.Pooled CreateCore(ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var pooled = Pool.GetOrCreate();
            pooled.Item.assignments.Clear();
            pooled.Item.symbol = symbol;
            pooled.Item.semanticModel = semanticModel;
            pooled.Item.cancellationToken = cancellationToken;
            foreach (var typeDeclaration in symbol.ContainingType.Declarations(cancellationToken))
            {
                pooled.Item.Visit(typeDeclaration);
            }

            return pooled;
        }
    }
}