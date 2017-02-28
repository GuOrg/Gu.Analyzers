namespace Gu.Analyzers
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class AssignedValueWalker : CSharpSyntaxWalker, IReadOnlyList<ExpressionSyntax>
    {
        private static readonly Pool<AssignedValueWalker> Pool = new Pool<AssignedValueWalker>(
            () => new AssignedValueWalker(),
            x =>
            {
                x.values.Clear();
                x.visitedLocations.Clear();
                x.refParameters.Clear();
                x.currentSymbol = null;
                x.context = null;
                x.semanticModel = null;
                x.cancellationToken = CancellationToken.None;
            });

        private readonly List<ExpressionSyntax> values = new List<ExpressionSyntax>();
        private readonly HashSet<SyntaxNode> visitedLocations = new HashSet<SyntaxNode>();
        private readonly HashSet<IParameterSymbol> refParameters = new HashSet<IParameterSymbol>(SymbolComparer.Default);
        private readonly MemberWalker memberWalker;

        private ISymbol currentSymbol;
        private SyntaxNode context;
        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;

        private AssignedValueWalker()
        {
            this.memberWalker = new MemberWalker(this);
        }

        public int Count => this.values.Count;

        public ExpressionSyntax this[int index] => this.values[index];

        public IEnumerator<ExpressionSyntax> GetEnumerator() => this.values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public override void Visit(SyntaxNode node)
        {
            if (this.ShouldVisit(node) != Result.Yes)
            {
                return;
            }

            base.Visit(node);
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            this.HandleAssignedValue(node, node.Initializer?.Value);
            base.VisitVariableDeclarator(node);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (node.Initializer != null &&
                this.visitedLocations.Add(node.Initializer))
            {
                var ctor = this.semanticModel.GetSymbolSafe(node.Initializer, this.cancellationToken);
                this.HandleInvoke(ctor, node.Initializer.ArgumentList);
            }
            else
            {
                var ctor = this.semanticModel.GetDeclaredSymbolSafe(node, this.cancellationToken);
                IMethodSymbol baseCtor;
                if (Constructor.TryGetDefault(ctor?.ContainingType?.BaseType, out baseCtor))
                {
                    this.HandleInvoke(baseCtor, null);
                }
            }

            var contextCtor = this.context?.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
            if (contextCtor != null)
            {
                if (contextCtor == node ||
                    node.IsRunBefore(contextCtor, this.semanticModel, this.cancellationToken))
                {
                    base.VisitConstructorDeclaration(node);
                }
            }
            else
            {
                base.VisitConstructorDeclaration(node);
            }
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            this.HandleAssignedValue(node.Left, node.Right);
            base.VisitAssignmentExpression(node);
        }

        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.LogicalNotExpression:
                    break;
                default:
                    this.HandleAssignedValue(node.Operand, node);
                    break;
            }

            base.VisitPrefixUnaryExpression(node);
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            this.HandleAssignedValue(node.Operand, node);
            base.VisitPostfixUnaryExpression(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (this.visitedLocations.Add(node))
            {
                base.VisitInvocationExpression(node);
                var method = this.semanticModel.GetSymbolSafe(node, this.cancellationToken);
                this.HandleInvoke(method, node.ArgumentList);
            }
        }

        public override void VisitArgument(ArgumentSyntax node)
        {
            if (this.visitedLocations.Add(node) &&
                (node.RefOrOutKeyword.IsKind(SyntaxKind.RefKeyword) ||
                 node.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword)))
            {
                var invocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
                var argSymbol = this.semanticModel.GetSymbolSafe(node.Expression, this.cancellationToken);
                if (invocation != null &&
                    (SymbolComparer.Equals(this.currentSymbol, argSymbol) ||
                     this.refParameters.Contains(argSymbol as IParameterSymbol)))
                {
                    var method = this.semanticModel.GetSymbolSafe(invocation, this.cancellationToken);
                    if (method != null &&
                        method.DeclaringSyntaxReferences.Length > 0)
                    {
                        foreach (var reference in method.DeclaringSyntaxReferences)
                        {
                            var methodDeclaration = reference.GetSyntax(this.cancellationToken) as MethodDeclarationSyntax;
                            ParameterSyntax parameterSyntax;
                            if (methodDeclaration.TryGetMatchingParameter(node, out parameterSyntax))
                            {
                                var parameter = this.semanticModel.GetDeclaredSymbolSafe(parameterSyntax, this.cancellationToken) as IParameterSymbol;
                                if (parameter != null)
                                {
                                    if (node.RefOrOutKeyword.IsKind(SyntaxKind.RefKeyword))
                                    {
                                        this.refParameters.Add(parameter).IgnoreReturnValue();
                                    }

                                    if (node.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword))
                                    {
                                        this.values.Add(invocation);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            base.VisitArgument(node);
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
                symbol is IParameterSymbol)
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
            if (this.currentSymbol == null)
            {
                return;
            }

            var type = (INamedTypeSymbol)this.semanticModel.GetDeclaredSymbolSafe(this.context?.FirstAncestorOrSelf<TypeDeclarationSyntax>(), this.cancellationToken);
            if (type == null)
            {
                return;
            }

            if (this.currentSymbol is IFieldSymbol ||
                this.currentSymbol is IPropertySymbol)
            {
                if (this.currentSymbol is IFieldSymbol)
                {
                    foreach (var reference in this.currentSymbol.DeclaringSyntaxReferences)
                    {
                        var fieldDeclarationSyntax = reference.GetSyntax(this.cancellationToken)
                                                              ?.FirstAncestorOrSelf<FieldDeclarationSyntax>();
                        if (fieldDeclarationSyntax != null)
                        {
                            this.Visit(fieldDeclarationSyntax);
                        }
                    }
                }

                if (this.currentSymbol is IPropertySymbol)
                {
                    foreach (var reference in this.currentSymbol.DeclaringSyntaxReferences)
                    {
                        var propertyDeclarationSyntax = reference.GetSyntax(this.cancellationToken)
                                                                 ?.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
                        if (propertyDeclarationSyntax?.Initializer?.Value != null)
                        {
                            this.values.Add(propertyDeclarationSyntax.Initializer.Value);
                        }
                    }
                }

                var contextCtor = this.context?.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
                foreach (var reference in type.DeclaringSyntaxReferences)
                {
                    using (var pooled = ConstructorsWalker.Create((TypeDeclarationSyntax)reference.GetSyntax(this.cancellationToken), this.semanticModel, this.cancellationToken))
                    {
                        if (this.context?.FirstAncestorOrSelf<ConstructorDeclarationSyntax>() == null &&
                            pooled.Item.Default != null)
                        {
                            this.Visit(pooled.Item.Default);
                        }

                        foreach (var creation in pooled.Item.ObjectCreations)
                        {
                            if (contextCtor == null ||
                                creation.Creates(contextCtor, true, this.semanticModel, this.cancellationToken))
                            {
                                if (this.visitedLocations.Add(creation))
                                {
                                    this.VisitObjectCreationExpression(creation);
                                    var method = this.semanticModel.GetSymbolSafe(creation, this.cancellationToken);
                                    this.HandleInvoke(method, creation.ArgumentList);
                                }
                            }
                        }

                        foreach (var ctor in pooled.Item.NonPrivateCtors)
                        {
                            if (contextCtor == null ||
                                ctor == contextCtor ||
                                contextCtor.IsRunBefore(ctor, this.semanticModel, this.cancellationToken))
                            {
                                this.Visit(ctor);
                            }
                        }
                    }
                }
            }

            var contextMember = this.context?.FirstAncestorOrSelf<MemberDeclarationSyntax>();
            if (contextMember != null &&
                (this.currentSymbol is ILocalSymbol ||
                 this.currentSymbol is IParameterSymbol))
            {
                this.Visit(contextMember);
            }
            else if (!(contextMember is ConstructorDeclarationSyntax))
            {
                while (type.Is(this.currentSymbol.ContainingType))
                {
                    foreach (var reference in type.DeclaringSyntaxReferences)
                    {
                        var typeDeclaration = (TypeDeclarationSyntax)reference.GetSyntax(this.cancellationToken);
                        this.memberWalker.Visit(typeDeclaration);
                    }

                    type = type.BaseType;
                }
            }

            this.values.PurgeDuplicates();
        }

        private void HandleAssignedValue(SyntaxNode assignee, ExpressionSyntax value)
        {
            if (value == null)
            {
                return;
            }

            var assignedSymbol = this.semanticModel.GetSymbolSafe(assignee, this.cancellationToken) ??
                                 this.semanticModel.GetDeclaredSymbolSafe(assignee, this.cancellationToken);
            if (assignedSymbol == null)
            {
                return;
            }

            var property = assignedSymbol as IPropertySymbol;
            if (!SymbolComparer.Equals(this.currentSymbol, property) &&
                (this.currentSymbol is IFieldSymbol || this.currentSymbol is IPropertySymbol) &&
                property != null &&
                Property.AssignsSymbolInSetter(
                    property,
                    this.currentSymbol,
                    this.semanticModel,
                    this.cancellationToken))
            {
                var before = this.values.Count;
                foreach (var reference in property.DeclaringSyntaxReferences)
                {
                    var declaration = (PropertyDeclarationSyntax)reference.GetSyntax(this.cancellationToken);
                    AccessorDeclarationSyntax setter;
                    if (declaration.TryGetSetAccessorDeclaration(out setter))
                    {
                        this.Visit(setter);
                    }
                }

                for (var i = before; i < this.values.Count; i++)
                {
                    var parameter =
                        this.semanticModel.GetSymbolSafe(this.values[i], this.cancellationToken) as IParameterSymbol;
                    if (Equals(parameter?.ContainingSymbol, property.SetMethod))
                    {
                        this.values[i] = value;
                    }
                }
            }
            else
            {
                if (SymbolComparer.Equals(this.currentSymbol, assignedSymbol) ||
                    this.refParameters.Contains(assignedSymbol as IParameterSymbol))
                {
                    this.values.Add(value);
                }
            }
        }

        private void HandleInvoke(ISymbol method, ArgumentListSyntax argumentList)
        {
            if (method != null)
            {
                var before = this.values.Count;
                if (method.ContainingType.Is(this.currentSymbol.ContainingType) ||
                    this.currentSymbol.ContainingType.Is(method.ContainingType))
                {
                    foreach (var reference in method.DeclaringSyntaxReferences)
                    {
                        base.Visit(reference.GetSyntax(this.cancellationToken));
                    }
                }

                if (before != this.values.Count &&
                    argumentList != null)
                {
                    for (var i = before; i < this.values.Count; i++)
                    {
                        var parameter = this.semanticModel.GetSymbolSafe(this.values[i], this.cancellationToken) as IParameterSymbol;
                        if (parameter != null &&
                            parameter.RefKind != RefKind.Out)
                        {
                            ExpressionSyntax arg;
                            if (argumentList.TryGetArgumentValue(parameter, this.cancellationToken, out arg))
                            {
                                this.values[i] = arg;
                            }
                        }
                    }
                }
            }
        }

        private Result ShouldVisit(SyntaxNode node)
        {
            if (this.currentSymbol is IPropertySymbol ||
                this.currentSymbol is IFieldSymbol)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.ExpressionStatement:
                        return this.context.SharesAncestor<ConstructorDeclarationSyntax>(node)
                                   ? node.IsBeforeInScope(this.context)
                                   : Result.Yes;
                    default:
                        return Result.Yes;
                }
            }

            switch (node.Kind())
            {
                case SyntaxKind.ExpressionStatement:
                    return this.context.SharesAncestor<MemberDeclarationSyntax>(node)
                               ? node.IsBeforeInScope(this.context)
                               : Result.Yes;
                default:
                    return Result.Yes;
            }
        }

        private class MemberWalker : CSharpSyntaxWalker
        {
            private readonly AssignedValueWalker inner;

            public MemberWalker(AssignedValueWalker inner)
            {
                this.inner = inner;
            }

            public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
            {
            }

            public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                if (this.inner.semanticModel.GetDeclaredSymbolSafe(node, this.inner.cancellationToken)
                        ?.DeclaredAccessibility != Accessibility.Private)
                {
                    base.VisitPropertyDeclaration(node);
                }
            }

            public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (this.inner.semanticModel.GetDeclaredSymbolSafe(node, this.inner.cancellationToken)
                        ?.DeclaredAccessibility != Accessibility.Private)
                {
                    this.inner.VisitMethodDeclaration(node);
                }
            }

            public override void VisitAccessorDeclaration(AccessorDeclarationSyntax node)
            {
                if (this.inner.semanticModel.GetDeclaredSymbolSafe(node, this.inner.cancellationToken)
                        ?.DeclaredAccessibility != Accessibility.Private)
                {
                    this.inner.VisitAccessorDeclaration(node);
                }
            }
        }
    }
}