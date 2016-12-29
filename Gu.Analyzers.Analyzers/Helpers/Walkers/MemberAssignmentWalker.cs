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
                x.assignedValues.Clear();
                x.symbol = null;
                x.semanticModel = null;
                x.cancellationToken = CancellationToken.None;
            });

        private readonly List<ExpressionSyntax> assignedValues = new List<ExpressionSyntax>();
        private ISymbol symbol;
        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;

        private MemberAssignmentWalker()
        {
        }

        public IReadOnlyList<ExpressionSyntax> AssignedValues => this.assignedValues;

        public static Pool<MemberAssignmentWalker>.Pooled AssignedValuesInType(IPropertySymbol property, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return CreateCore(property, property.ContainingType.Declarations(cancellationToken), semanticModel, cancellationToken);
        }

        public static Pool<MemberAssignmentWalker>.Pooled AssignedValuesInType(IFieldSymbol field, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return CreateCore(field, field.ContainingType.Declarations(cancellationToken), semanticModel, cancellationToken);
        }

        public static Pool<MemberAssignmentWalker>.Pooled AssignedValuesInScope(ISymbol member, SyntaxNode scope, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var field = member as IFieldSymbol;
            if (field != null)
            {
                return CreateCore(field, new[] { scope }, semanticModel, cancellationToken);
            }

            var property = member as IPropertySymbol;
            if (property != null)
            {
                return CreateCore(property, new[] { scope }, semanticModel, cancellationToken);
            }

            return null;
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var left = this.semanticModel.GetSymbolSafe(node.Left, this.cancellationToken);
            if (ReferenceEquals(left, this.symbol))
            {
                this.assignedValues.Add(node.Right);
            }

            base.VisitAssignmentExpression(node);
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            var operand = this.semanticModel.GetSymbolSafe(node.Operand, this.cancellationToken);
            if (this.symbol.Equals(operand))
            {
                this.assignedValues.Add(node.Operand);
            }

            base.VisitPostfixUnaryExpression(node);
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            if (node.Initializer != null &&
                ReferenceEquals(this.semanticModel.GetDeclaredSymbolSafe(node, this.cancellationToken), this.symbol))
            {
                this.assignedValues.Add(node.Initializer.Value);
            }

            base.VisitVariableDeclarator(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (node.Initializer != null &&
                ReferenceEquals(this.semanticModel.GetDeclaredSymbolSafe(node, this.cancellationToken), this.symbol))
            {
                this.assignedValues.Add(node.Initializer.Value);
            }

            base.VisitPropertyDeclaration(node);
        }

        private static Pool<MemberAssignmentWalker>.Pooled CreateCore(ISymbol symbol, IEnumerable<SyntaxNode> nodes, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var pooled = Pool.GetOrCreate();
            pooled.Item.assignedValues.Clear();
            pooled.Item.symbol = symbol;
            pooled.Item.semanticModel = semanticModel;
            pooled.Item.cancellationToken = cancellationToken;
            foreach (var node in nodes)
            {
                pooled.Item.Visit(node);
            }

            return pooled;
        }
    }
}