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
                x.symbols.Clear();
                x.assignedValues.Clear();
                x.symbol = null;
                x.semanticModel = null;
                x.cancellationToken = CancellationToken.None;
            });

        private readonly List<ExpressionSyntax> assignedValues = new List<ExpressionSyntax>();
        private readonly HashSet<ISymbol> symbols = new HashSet<ISymbol>();

        private ISymbol symbol;
        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;
        private bool isSamplingRetunValues;

        private MemberAssignmentWalker()
        {
        }

        public IReadOnlyList<ExpressionSyntax> AssignedValues => this.assignedValues;

        public bool IsPotentiallyAssignedFromOutside
        {
            get
            {
                foreach (var s in this.symbols)
                {
                    if (s.ContainingType != this.symbol.ContainingType)
                    {
                        return true;
                    }

                    var field = s as IFieldSymbol;
                    if (field?.IsReadOnly == false &&
                        field.DeclaredAccessibility != Accessibility.Private)
                    {
                        return true;
                    }

                    var propertySymbol = s as IPropertySymbol;
                    if (propertySymbol?.IsReadOnly == false &&
                        propertySymbol.DeclaredAccessibility != Accessibility.Private)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public static Pool<MemberAssignmentWalker>.Pooled AssignedValuesInType(IPropertySymbol property, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return CreateCore(property, property.ContainingType.Declarations(cancellationToken), semanticModel, cancellationToken);
        }

        public static Pool<MemberAssignmentWalker>.Pooled AssignedValuesInType(IFieldSymbol field, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return CreateCore(field, field.ContainingType.Declarations(cancellationToken), semanticModel, cancellationToken);
        }

        public static Pool<MemberAssignmentWalker>.Pooled AssignedValuesInScope(ISymbol parameter, SyntaxNode scope, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return CreateCore(parameter, new[] { scope }, semanticModel, cancellationToken);
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var left = this.semanticModel.GetSymbolSafe(node.Left, this.cancellationToken);
            if (this.symbol.Equals(left))
            {
                this.AddPropertyIfInSetter(node);
                this.assignedValues.Add(node.Right);
            }

            base.VisitAssignmentExpression(node);
        }

        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            var operand = this.semanticModel.GetSymbolSafe(node.Operand, this.cancellationToken);
            if (this.symbol.Equals(operand))
            {
                this.AddPropertyIfInSetter(node);
                this.assignedValues.Add(node.Operand);
            }

            base.VisitPrefixUnaryExpression(node);
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            var operand = this.semanticModel.GetSymbolSafe(node.Operand, this.cancellationToken);
            if (this.symbol.Equals(operand))
            {
                this.AddPropertyIfInSetter(node);
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

        public override void VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
        {
            var propertyDeclaration = node.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
            if (propertyDeclaration != null)
            {
                var property = this.semanticModel.GetDeclaredSymbolSafe(propertyDeclaration, this.cancellationToken);
                if (this.symbols.Contains(property))
                {
                    var returnedSymbol = this.semanticModel.GetSymbolSafe(node.Expression, this.cancellationToken);
                    if (returnedSymbol != null)
                    {
                        this.symbols.Add(returnedSymbol);
                    }
                }
            }

            base.VisitArrowExpressionClause(node);
        }

        public override void VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            if (node.IsKind(SyntaxKind.GetAccessorDeclaration))
            {
                this.isSamplingRetunValues = true;
                base.VisitAccessorDeclaration(node);
                this.isSamplingRetunValues = false;
                return;
            }

            base.VisitAccessorDeclaration(node);
        }

        public override void VisitReturnStatement(ReturnStatementSyntax node)
        {
            if (this.isSamplingRetunValues)
            {
                var returnedSymbol = this.semanticModel.GetSymbolSafe(node.Expression, this.cancellationToken);
                if (returnedSymbol != null)
                {
                    this.symbols.Add(returnedSymbol);
                }
            }

            base.VisitReturnStatement(node);
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
            pooled.Item.symbol = symbol;
            pooled.Item.semanticModel = semanticModel;
            pooled.Item.cancellationToken = cancellationToken;
            pooled.Item.symbols.Add(symbol);

            var count = 0;
            while (count != pooled.Item.symbols.Count)
            {
                pooled.Item.assignedValues.Clear();
                // ReSharper disable once PossibleMultipleEnumeration
                foreach (var node in nodes)
                {
                    pooled.Item.Visit(node);
                }

                count = pooled.Item.symbols.Count;
            }

            return pooled;
        }

        private void AddPropertyIfInSetter(SyntaxNode assignment)
        {
            var setter = assignment.FirstAncestorOrSelf<AccessorDeclarationSyntax>();
            if (setter?.IsKind(SyntaxKind.SetAccessorDeclaration) == true)
            {
                var property = this.semanticModel.GetDeclaredSymbol(setter.FirstAncestorOrSelf<PropertyDeclarationSyntax>());
                if (property?.SetMethod != null)
                {
                    this.symbols.Add(property);
                }
            }
        }
    }
}