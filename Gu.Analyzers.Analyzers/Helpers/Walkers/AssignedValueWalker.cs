namespace Gu.Analyzers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class AssignedValueWalker : CSharpSyntaxWalker, IEnumerable<Assignment>
    {
        private static readonly Pool<AssignedValueWalker> Pool = new Pool<AssignedValueWalker>(
            () => new AssignedValueWalker(),
            x =>
            {
                x.values.Clear();
                x.visitedLocations.Clear();
                x.currentSymbol = null;
                x.context = null;
                x.semanticModel = null;
                x.cancellationToken = CancellationToken.None;
            });

        private readonly List<Assignment> values = new List<Assignment>();
        private readonly HashSet<ISymbol> checkedSymbols = new HashSet<ISymbol>();
        private readonly HashSet<SyntaxNode> visitedLocations = new HashSet<SyntaxNode>();

        private ISymbol currentSymbol;
        private SyntaxNode context;
        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;
        private bool isSamplingRetunValues;

        private AssignedValueWalker()
        {
        }

        public IEnumerator<Assignment> GetEnumerator() => new AssignmentEnumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public override void Visit(SyntaxNode node)
        {
            if (!this.IsBeforeInScope(node))
            {
                return;
            }

            base.Visit(node);
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            if (node.Initializer != null &&
                SymbolComparer.Equals(this.currentSymbol, this.semanticModel.GetDeclaredSymbolSafe(node, this.cancellationToken)))
            {
                this.values.Add(new Assignment(this.currentSymbol, node.Initializer.Value));
            }

            base.VisitVariableDeclarator(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (node.Initializer != null &&
                SymbolComparer.Equals(this.currentSymbol, this.semanticModel.GetDeclaredSymbolSafe(node, this.cancellationToken)))
            {
                this.values.Add(new Assignment(this.currentSymbol, node.Initializer.Value));
            }

            base.VisitPropertyDeclaration(node);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (!this.visitedLocations.Add(node))
            {
                return;
            }

            if (node.Initializer != null)
            {
                var ctor = this.semanticModel.GetSymbolSafe(node.Initializer, this.cancellationToken);
                if (ctor != null)
                {
                    foreach (var reference in ctor.DeclaringSyntaxReferences)
                    {
                        this.Visit(reference.GetSyntax(this.cancellationToken));
                    }
                }
            }
            else
            {
                var ctor = this.semanticModel.GetDeclaredSymbolSafe(node, this.cancellationToken);
                IMethodSymbol baseCtor;
                if (Constructor.TryGetDefault(ctor?.ContainingType.BaseType, out baseCtor))
                {
                    foreach (var reference in baseCtor.DeclaringSyntaxReferences)
                    {
                        this.Visit(reference.GetSyntax(this.cancellationToken));
                    }
                }
            }

            base.VisitConstructorDeclaration(node);
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var left = this.semanticModel.GetSymbolSafe(node.Left, this.cancellationToken);
            if (SymbolComparer.Equals(this.currentSymbol, left))
            {
                this.values.Add(new Assignment(this.currentSymbol, node.Right));
            }
            else if (left is IPropertySymbol)
            {
                this.VisitSetter(left);
            }

            base.VisitAssignmentExpression(node);
        }

        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            var operand = this.semanticModel.GetSymbolSafe(node.Operand, this.cancellationToken);
            if (SymbolComparer.Equals(this.currentSymbol, operand))
            {
                this.values.Add(new Assignment(this.currentSymbol, node));
            }
            else if (operand is IPropertySymbol)
            {
                this.VisitSetter(operand);
            }

            base.VisitPrefixUnaryExpression(node);
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            var operand = this.semanticModel.GetSymbolSafe(node.Operand, this.cancellationToken);
            if (SymbolComparer.Equals(this.currentSymbol, operand))
            {
                this.values.Add(new Assignment(this.currentSymbol, node));
            }
            else if (operand is IPropertySymbol)
            {
                this.VisitSetter(operand);
            }

            base.VisitPostfixUnaryExpression(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (this.currentSymbol is IFieldSymbol ||
                this.currentSymbol is IPropertySymbol)
            {
                if (this.visitedLocations.Add(node))
                {
                    var method = this.semanticModel.GetSymbolSafe(node, this.cancellationToken);
                    if (method != null)
                    {
                        if (method.ContainingType.Is(this.currentSymbol.ContainingType) ||
                            this.currentSymbol.ContainingType.Is(method.ContainingType))
                        {
                            foreach (var reference in method.DeclaringSyntaxReferences)
                            {
                                this.Visit(reference.GetSyntax(this.cancellationToken));
                            }
                        }
                    }
                }
            }

            base.VisitInvocationExpression(node);
        }

        public override void VisitArgument(ArgumentSyntax node)
        {
            if (node.RefOrOutKeyword.IsKind(SyntaxKind.RefKeyword) ||
                node.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword))
            {
                var invocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
                if (invocation != null &&
                    SymbolComparer.Equals(this.currentSymbol, this.semanticModel.GetSymbolSafe(node.Expression, this.cancellationToken)))
                {
                    this.values.Add(new Assignment(this.currentSymbol, invocation));
                }
            }

            base.VisitArgument(node);
        }

        public override void VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
        {
            if (this.isSamplingRetunValues)
            {
                this.values.Add(new Assignment(this.currentSymbol, node.Expression));
            }

            base.VisitArrowExpressionClause(node);
        }

        public override void VisitReturnStatement(ReturnStatementSyntax node)
        {
            if (this.isSamplingRetunValues)
            {
                this.values.Add(new Assignment(this.currentSymbol, node.Expression));
            }

            base.VisitReturnStatement(node);
        }

        internal static Pool<AssignedValueWalker>.Pooled Create(IPropertySymbol property, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return CreateCore(property, null, semanticModel, cancellationToken);
        }

        internal static Pool<AssignedValueWalker>.Pooled Create(IFieldSymbol field, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return CreateCore(field, null, semanticModel, cancellationToken);
        }

        internal static Pool<AssignedValueWalker>.Pooled Create(ExpressionSyntax value, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var symbol = semanticModel.GetSymbolSafe(value, cancellationToken);
            if (symbol is IFieldSymbol ||
                symbol is IPropertySymbol ||
                symbol is ILocalSymbol ||
                symbol is IParameterSymbol ||
                symbol is IMethodSymbol)
            {
                return CreateCore(symbol, value, semanticModel, cancellationToken);
            }

            return Pool.GetOrCreate();
        }

        internal static Pool<AssignedValueWalker>.Pooled Create(ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
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

        internal static Pool<AssignedValueWalker>.Pooled CreateEmpty(SyntaxNode context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var pooled = Pool.GetOrCreate();
            pooled.Item.context = context;
            pooled.Item.semanticModel = semanticModel;
            pooled.Item.cancellationToken = cancellationToken;
            return pooled;
        }

        internal bool AddAssignedValuesFor(ExpressionSyntax assignedValue)
        {
            var symbol = this.semanticModel.GetSymbolSafe(assignedValue, this.cancellationToken);
            if (symbol == null ||
                !this.visitedLocations.Add(assignedValue))
            {
                return false;
            }

            this.currentSymbol = symbol;
            var before = this.values.Count;
            this.Run();
            var parameter = this.currentSymbol as IParameterSymbol;
            if (parameter?.Name == "value")
            {
                this.currentSymbol = (parameter.ContainingSymbol as IMethodSymbol)?.AssociatedSymbol as IPropertySymbol;
                this.Run();
            }

            return before != this.values.Count;
        }

        internal bool AddReturnValues(IPropertySymbol property, SyntaxNode call)
        {
            if (property == null ||
                property.DeclaringSyntaxReferences.Length == 0 ||
                property.GetMethod == null ||
                !this.visitedLocations.Add(call))
            {
                return false;
            }

            if (!this.checkedSymbols.Add(property.GetMethod))
            {
                foreach (var assignment in this.values)
                {
                    if (Equals(assignment.Symbol, property))
                    {
                        return true;
                    }
                }

                return false;
            }

            this.currentSymbol = property.GetMethod;
            var before = this.values.Count;
            foreach (var reference in property.DeclaringSyntaxReferences)
            {
                var declaration = (PropertyDeclarationSyntax)reference.GetSyntax(this.cancellationToken);
                AccessorDeclarationSyntax getter;
                if (declaration.TryGetGetAccessorDeclaration(out getter))
                {
                    this.isSamplingRetunValues = true;
                    this.Visit(getter);
                    this.isSamplingRetunValues = false;
                }
            }

            return before != this.values.Count;
        }

        internal bool AddReturnValues(IMethodSymbol method, SyntaxNode call)
        {
            if (method == null ||
                method.DeclaringSyntaxReferences.Length == 0 ||
                !this.visitedLocations.Add(call))
            {
                return false;
            }

            if (!this.checkedSymbols.Add(method))
            {
                foreach (var assignment in this.values)
                {
                    if (Equals(assignment.Symbol, method))
                    {
                        return true;
                    }
                }

                return false;
            }

            this.currentSymbol = method;
            var before = this.values.Count;
            foreach (var reference in method.DeclaringSyntaxReferences)
            {
                var declaration = (MethodDeclarationSyntax)reference.GetSyntax(this.cancellationToken);
                this.isSamplingRetunValues = true;
                this.Visit(declaration);
                this.isSamplingRetunValues = false;
            }

            return before != this.values.Count;
        }

        private static Pool<AssignedValueWalker>.Pooled CreateCore(ISymbol symbol, SyntaxNode context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (symbol == null)
            {
                return Pool.GetOrCreate();
            }

            var pooled = Pool.GetOrCreate();
            pooled.Item.currentSymbol = symbol;
            pooled.Item.context = context;
            pooled.Item.semanticModel = semanticModel;
            pooled.Item.cancellationToken = cancellationToken;
            if (context != null)
            {
                pooled.Item.Run();
            }
            else
            {
                foreach (var reference in symbol.DeclaringSyntaxReferences)
                {
                    pooled.Item.context = symbol is IFieldSymbol || symbol is IPropertySymbol
                                              ? reference.GetSyntax(cancellationToken)
                                                         .FirstAncestor<TypeDeclarationSyntax>()
                                              : reference.GetSyntax(cancellationToken)
                                                         .FirstAncestor<MemberDeclarationSyntax>();
                    pooled.Item.Run();
                }
            }

            return pooled;
        }

        private void Run()
        {
            if (this.currentSymbol == null ||
                !this.checkedSymbols.Add(this.currentSymbol))
            {
                return;
            }

            if (this.currentSymbol is IFieldSymbol ||
                this.currentSymbol is IPropertySymbol)
            {
                foreach (var reference in this.currentSymbol.DeclaringSyntaxReferences)
                {
                    var memberDeclaration = reference.GetSyntax(this.cancellationToken)?.FirstAncestorOrSelf<MemberDeclarationSyntax>();
                    if (memberDeclaration != null)
                    {
                        var oldSymbol = this.currentSymbol;
                        this.AddReturnValues(this.currentSymbol as IPropertySymbol, memberDeclaration).IgnoreReturnValue();
                        this.currentSymbol = oldSymbol;
                        this.Visit(memberDeclaration);
                    }
                }
            }

            var ctor = this.context?.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
            if (ctor != null)
            {
                this.Visit(ctor);
            }
            else
            {
                var typeDeclaration = this.context?.FirstAncestorOrSelf<TypeDeclarationSyntax>();
                if (typeDeclaration == null)
                {
                    return;
                }

                if (this.currentSymbol is IFieldSymbol ||
                    this.currentSymbol is IPropertySymbol)
                {
                    var type = (INamedTypeSymbol)this.semanticModel.GetDeclaredSymbolSafe(typeDeclaration, this.cancellationToken);
                    foreach (var typeCtor in type.Constructors)
                    {
                        if (Constructor.IsCalledByOther(typeCtor, this.semanticModel, this.cancellationToken))
                        {
                            continue;
                        }

                        if (typeCtor.DeclaringSyntaxReferences.Length == 0)
                        {
                            IMethodSymbol defaultCtor;
                            if (Constructor.TryGetDefault(type.BaseType, out defaultCtor))
                            {
                                foreach (var reference in defaultCtor.DeclaringSyntaxReferences)
                                {
                                    this.Visit(reference.GetSyntax(this.cancellationToken));
                                }
                            }
                        }

                        foreach (var reference in typeCtor.DeclaringSyntaxReferences)
                        {
                            ctor = (ConstructorDeclarationSyntax)reference.GetSyntax(this.cancellationToken);
                            this.Visit(ctor);
                        }
                    }

                    this.Visit(typeDeclaration);
                }
                else
                {
                    var memnber = this.context?.FirstAncestorOrSelf<MemberDeclarationSyntax>();
                    this.Visit(memnber ?? typeDeclaration);
                }
            }
        }

        private void VisitSetter(ISymbol left)
        {
            if (left is IPropertySymbol)
            {
                foreach (var reference in left.DeclaringSyntaxReferences)
                {
                    AccessorDeclarationSyntax setter;
                    if ((reference.GetSyntax(this.cancellationToken) as PropertyDeclarationSyntax).TryGetSetAccessorDeclaration(out setter))
                    {
                        this.Visit(setter);
                    }
                }
            }
        }

        private bool IsBeforeInScope(SyntaxNode node)
        {
            if (this.context == null ||
                node is BlockSyntax ||
                node.FirstAncestorOrSelf<StatementSyntax>() == null)
            {
                return true;
            }

            if (this.currentSymbol is IParameterSymbol ||
                this.currentSymbol is ILocalSymbol)
            {
                return node.IsBeforeInScope(this.context);
            }

            if (
                !this.context.SharesAncestor<ConstructorDeclarationSyntax>(node))
            {
                return true;
            }

            return node.IsBeforeInScope(this.context);
        }

        private class AssignmentEnumerator : IEnumerator<Assignment>
        {
            private readonly AssignedValueWalker walker;
            private int index = -1;

            public AssignmentEnumerator(AssignedValueWalker walker)
            {
                this.walker = walker;
            }

            public Assignment Current => this.walker.values[this.index];

            object IEnumerator.Current => this.Current;

            public bool MoveNext()
            {
                if (this.index >= this.walker.values.Count - 1)
                {
                    return false;
                }

                this.index++;
                return true;
            }

            public void Reset()
            {
                this.index = -1;
            }

            void IDisposable.Dispose()
            {
            }
        }
    }
}