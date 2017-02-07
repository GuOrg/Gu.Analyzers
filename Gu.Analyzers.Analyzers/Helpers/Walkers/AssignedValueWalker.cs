namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class AssignedValueWalker : CSharpSyntaxWalker
    {
        private static readonly Pool<AssignedValueWalker> Pool = new Pool<AssignedValueWalker>(
            () => new AssignedValueWalker(),
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

        private AssignedValueWalker()
        {
        }

        public IReadOnlyList<ExpressionSyntax> AssignedValues => this.assignedValues;

        public static Pool<AssignedValueWalker>.Pooled AssignedValuesInType(IPropertySymbol property, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return CreateCore(property, property.ContainingType.Declarations(cancellationToken), semanticModel, cancellationToken);
        }

        public static Pool<AssignedValueWalker>.Pooled AssignedValuesInType(IFieldSymbol field, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return CreateCore(field, field.ContainingType.Declarations(cancellationToken), semanticModel, cancellationToken);
        }

        public static Pool<AssignedValueWalker>.Pooled AssignedValuesInType(ILocalSymbol local, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return CreateCore(local, local.ContainingSymbol.Declarations(cancellationToken), semanticModel, cancellationToken);
        }

        public static Pool<AssignedValueWalker>.Pooled AssignedValuesInType(IParameterSymbol parameter, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return CreateCore(parameter, parameter.ContainingSymbol.Declarations(cancellationToken), semanticModel, cancellationToken);
        }

        public static Pool<AssignedValueWalker>.Pooled AssignedValuesInType(ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (symbol is IFieldSymbol ||
                symbol is IPropertySymbol)
            {
                return CreateCore(symbol, symbol.ContainingType.Declarations(cancellationToken), semanticModel, cancellationToken);
            }

            if (symbol is ILocalSymbol ||
                symbol is IParameterSymbol)
            {
                return CreateCore(symbol, symbol.ContainingSymbol.Declarations(cancellationToken), semanticModel, cancellationToken);
            }

            return Pool.GetOrCreate();
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var left = this.semanticModel.GetSymbolSafe(node.Left, this.cancellationToken);
            if (this.symbols.Contains(left))
            {
                this.AddPropertyIfInSetter(node);
                this.assignedValues.Add(node.Right);
            }

            base.VisitAssignmentExpression(node);
        }

        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            var operand = this.semanticModel.GetSymbolSafe(node.Operand, this.cancellationToken);
            if (this.symbols.Contains(operand))
            {
                this.AddPropertyIfInSetter(node);
                this.assignedValues.Add(node.Operand);
            }

            base.VisitPrefixUnaryExpression(node);
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            var operand = this.semanticModel.GetSymbolSafe(node.Operand, this.cancellationToken);
            if (this.symbols.Contains(operand))
            {
                this.AddPropertyIfInSetter(node);
                this.assignedValues.Add(node.Operand);
            }

            base.VisitPostfixUnaryExpression(node);
        }

        public override void VisitArgument(ArgumentSyntax node)
        {
            if (node.RefOrOutKeyword.IsKind(SyntaxKind.RefKeyword) ||
                node.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword))
            {
                var invocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
                if (invocation != null &&
                    this.symbols.Contains(this.semanticModel.GetSymbolSafe(node.Expression, this.cancellationToken)))
                {
                    this.assignedValues.Add(node.Expression);
                    var method = (IMethodSymbol)this.semanticModel.GetSymbolSafe(invocation, this.cancellationToken);
                    if (method == null)
                    {
                        return;
                    }

                    if (node.NameColon?.Name != null)
                    {
                        foreach (var parameter in method.Parameters)
                        {
                            if (parameter.Name == node.NameColon.Name.Identifier.ValueText)
                            {
                                if (this.symbols.Add(parameter))
                                {
                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        var argumentList = node.FirstAncestorOrSelf<ArgumentListSyntax>();
                        var parameter = method.Parameters[argumentList.Arguments.IndexOf(node)];
                        if (this.symbols.Add(parameter))
                        {
                            return;
                        }
                    }
                }
            }

            base.VisitArgument(node);
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            if (node.Initializer != null &&
                this.symbols.Contains(this.semanticModel.GetDeclaredSymbolSafe(node, this.cancellationToken)))
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
                        if (this.symbols.Add(returnedSymbol))
                        {
                            return;
                        }
                    }
                }
            }

            base.VisitArrowExpressionClause(node);
        }

        public override void VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            if (node.IsKind(SyntaxKind.GetAccessorDeclaration))
            {
                var propertyDeclaration = node.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
                if (propertyDeclaration != null)
                {
                    var property = this.semanticModel.GetDeclaredSymbolSafe(propertyDeclaration, this.cancellationToken);
                    if (this.symbols.Contains(property))
                    {
                        this.isSamplingRetunValues = true;
                        base.VisitAccessorDeclaration(node);
                        this.isSamplingRetunValues = false;
                        return;
                    }
                }
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
                    if (this.symbols.Add(returnedSymbol))
                    {
                        return;
                    }
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

        private static Pool<AssignedValueWalker>.Pooled CreateCore(ISymbol symbol, IEnumerable<SyntaxNode> nodes, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var pooled = Pool.GetOrCreate();
            pooled.Item.symbol = symbol;
            pooled.Item.semanticModel = semanticModel;
            pooled.Item.cancellationToken = cancellationToken;
            pooled.Item.symbols.Add(symbol).IgnoreReturnValue();

            using (var pooledNodes = SetPool<SyntaxNode>.Create())
            {
                pooledNodes.Item.UnionWith(nodes);
                var count = 0;
                while (count != pooled.Item.symbols.Count)
                {
                    count = pooled.Item.symbols.Count;
                    pooled.Item.assignedValues.Clear();
                    foreach (var assignedSymbol in pooled.Item.symbols)
                    {
                        if (!IsChecked(assignedSymbol as IParameterSymbol, pooledNodes.Item))
                        {
                            foreach (var declaration in assignedSymbol.ContainingSymbol.Declarations(cancellationToken))
                            {
                                pooledNodes.Item.Add(declaration).IgnoreReturnValue();
                            }
                        }
                    }

                    //// ReSharper disable once PossibleMultipleEnumeration
                    foreach (var node in pooledNodes.Item)
                    {
                        pooled.Item.Visit(node);
                    }
                }
            }

            return pooled;
        }

        private static bool IsChecked(IParameterSymbol parameter, HashSet<SyntaxNode> nodes)
        {
            if (parameter == null)
            {
                return true;
            }

            foreach (var reference in parameter.ContainingSymbol.DeclaringSyntaxReferences)
            {
                foreach (var syntaxNode in nodes)
                {
                    if (syntaxNode.Span.Contains(reference.Span))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void AddPropertyIfInSetter(SyntaxNode assignment)
        {
            var setter = assignment.FirstAncestorOrSelf<AccessorDeclarationSyntax>();
            if (setter?.IsKind(SyntaxKind.SetAccessorDeclaration) == true)
            {
                var property = this.semanticModel.GetDeclaredSymbolSafe(setter.FirstAncestorOrSelf<BasePropertyDeclarationSyntax>(), this.cancellationToken) as IPropertySymbol;
                if (property?.SetMethod != null)
                {
                    this.symbols.Add(property).IgnoreReturnValue();
                }
            }
        }
    }
}