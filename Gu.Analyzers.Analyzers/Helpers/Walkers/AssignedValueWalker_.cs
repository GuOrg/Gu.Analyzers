namespace Gu.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [Obsolete]
    internal sealed class AssignedValueWalker_ : CSharpSyntaxWalker
    {
        private static readonly Pool<AssignedValueWalker_> Pool = new Pool<AssignedValueWalker_>(
            () => new AssignedValueWalker_(),
            x =>
            {
                x.symbols.Clear();
                x.assignedValues.Clear();
                x.ctorsRunBefore.Clear();
                x.symbol = null;
                x.context = null;
                x.semanticModel = null;
                x.cancellationToken = CancellationToken.None;
            });

        private readonly List<ExpressionSyntax> assignedValues = new List<ExpressionSyntax>();
        private readonly HashSet<ISymbol> symbols = new HashSet<ISymbol>(SymbolComparer.Default);
        private readonly HashSet<IMethodSymbol> ctorsRunBefore = new HashSet<IMethodSymbol>(SymbolComparer.Default);

        private ISymbol symbol;
        private SyntaxNode context;
        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;
        private bool isSamplingRetunValues;

        private AssignedValueWalker_()
        {
        }

        public IReadOnlyList<ExpressionSyntax> AssignedValues => this.assignedValues;

        [Obsolete("Remove this, use the overload with expression to capture context for figuring out what ctors to run.")]
        public static Pool<AssignedValueWalker_>.Pooled Create(IPropertySymbol property, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return CreateCore(property, null, semanticModel, cancellationToken);
        }

        [Obsolete("Remove this, use the overload with expression to capture context for figuring out what ctors to run.")]
        public static Pool<AssignedValueWalker_>.Pooled Create(IFieldSymbol field, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return CreateCore(field, null, semanticModel, cancellationToken);
        }

        public static Pool<AssignedValueWalker_>.Pooled Create(ExpressionSyntax value, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var symbol = semanticModel.GetSymbolSafe(value, cancellationToken);
            if (symbol is IFieldSymbol ||
                symbol is IPropertySymbol ||
                symbol is ILocalSymbol ||
                symbol is IParameterSymbol)
            {
                return CreateCore(symbol, value, semanticModel, cancellationToken);
            }

            return Pool.GetOrCreate();
        }

        [Obsolete("Remove this, use the overload with expression to capture context for figuring out what ctors to run.")]
        public static Pool<AssignedValueWalker_>.Pooled Create(ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (symbol is IFieldSymbol ||
                symbol is IPropertySymbol ||
                symbol is ILocalSymbol ||
                symbol is IParameterSymbol)
            {
                return CreateCore(symbol, null, semanticModel, cancellationToken);
            }

            return Pool.GetOrCreate();
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            if (this.IsBeforeInScope(node))
            {
                var left = this.semanticModel.GetSymbolSafe(node.Left, this.cancellationToken);
                this.HandleBackingField(left as IPropertySymbol);
                if (this.symbols.Contains(left))
                {
                    this.AddPropertyIfInSetter(node);
                    this.assignedValues.Add(node.Right);
                }
            }

            base.VisitAssignmentExpression(node);
        }

        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            if (this.IsBeforeInScope(node))
            {
                var operand = this.semanticModel.GetSymbolSafe(node.Operand, this.cancellationToken);
                this.HandleBackingField(operand as IPropertySymbol);
                if (this.symbols.Contains(operand))
                {
                    this.AddPropertyIfInSetter(node);
                    this.assignedValues.Add(node.Operand);
                }
            }

            base.VisitPrefixUnaryExpression(node);
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            if (this.IsBeforeInScope(node))
            {
                var operand = this.semanticModel.GetSymbolSafe(node.Operand, this.cancellationToken);
                this.HandleBackingField(operand as IPropertySymbol);
                if (this.symbols.Contains(operand))
                {
                    this.AddPropertyIfInSetter(node);
                    this.assignedValues.Add(node.Operand);
                }
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
                    this.symbols.Contains(this.semanticModel.GetSymbolSafe(node.Expression, this.cancellationToken)) &&
                    this.IsBeforeInScope(node))
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

        private static Pool<AssignedValueWalker_>.Pooled CreateCore(ISymbol symbol, SyntaxNode context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var pooled = Pool.GetOrCreate();
            pooled.Item.symbol = symbol;
            pooled.Item.context = context;
            pooled.Item.semanticModel = semanticModel;
            pooled.Item.cancellationToken = cancellationToken;
            pooled.Item.symbols.Add(symbol).IgnoreReturnValue();
            Constructor.AddRunBefore(context, pooled.Item.ctorsRunBefore, semanticModel, cancellationToken);
            using (var pooledNodes = SetPool<SyntaxNode>.Create())
            {
                var count = 0;
                while (count != pooled.Item.symbols.Count)
                {
                    count = pooled.Item.symbols.Count;
                    pooled.Item.assignedValues.Clear();
                    foreach (var assignedSymbol in pooled.Item.symbols)
                    {
                        if (!IsChecked(assignedSymbol, pooledNodes.Item))
                        {
                            foreach (var reference in assignedSymbol.ContainingSymbol.DeclaringSyntaxReferences)
                            {
                                pooledNodes.Item.Add(reference.GetSyntax(cancellationToken)).IgnoreReturnValue();
                            }
                        }
                    }

                    foreach (var node in pooledNodes.Item)
                    {
                        pooled.Item.Visit(node);
                    }
                }
            }

            return pooled;
        }

        private static bool IsChecked(ISymbol symbol, HashSet<SyntaxNode> nodes)
        {
            if (symbol == null)
            {
                return true;
            }

            foreach (var reference in symbol.ContainingSymbol.DeclaringSyntaxReferences)
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

        private void HandleBackingField(IPropertySymbol property)
        {
            if (property == null ||
                property.DeclaringSyntaxReferences.Length == 0)
            {
                return;
            }

            foreach (var reference in property.DeclaringSyntaxReferences)
            {
                var declaration = reference.GetSyntax(this.cancellationToken) as PropertyDeclarationSyntax;
                AccessorDeclarationSyntax setter;
                if (declaration.TryGetSetAccessorDeclaration(out setter))
                {
                    using (var pooled = IdentifierNameWalker.Create(setter))
                    {
                        foreach (var name in pooled.Item.IdentifierNames)
                        {
                            var assignment = name.FirstAncestor<AssignmentExpressionSyntax>();
                            if (assignment != null &&
                                assignment.Left.Span.Contains(name.Span))
                            {
                                var assignedSymbol = this.semanticModel.GetSymbolSafe(assignment.Left, this.cancellationToken);
                                if (this.symbols.Contains(assignedSymbol))
                                {
                                    this.symbols.Add(property).IgnoreReturnValue();
                                }
                                else if (this.symbols.Contains(property))
                                {
                                    this.symbols.Add(assignedSymbol).IgnoreReturnValue();
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool IsBeforeInScope(SyntaxNode node)
        {
            if (this.context == null)
            {
                return true;
            }

            var ctor = node.FirstAncestor<ConstructorDeclarationSyntax>();
            var contextCtor = this.context.FirstAncestor<ConstructorDeclarationSyntax>();
            if (ctor != null)
            {
                if (ctor != contextCtor)
                {
                    return this.ctorsRunBefore.Contains(this.semanticModel.GetDeclaredSymbolSafe(ctor, this.cancellationToken));
                }

                return node.IsBeforeInScope(this.context);
            }

            return this.context.FirstAncestor<MemberDeclarationSyntax>() != node.FirstAncestor<MemberDeclarationSyntax>() ||
                   node.IsBeforeInScope(this.context);
        }
    }
}