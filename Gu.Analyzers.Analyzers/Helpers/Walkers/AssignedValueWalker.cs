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
                x.parameters.Clear();
                x.currentSymbol = null;
                x.context = null;
                x.semanticModel = null;
                x.cancellationToken = CancellationToken.None;
            });

        private readonly List<ExpressionSyntax> values = new List<ExpressionSyntax>();
        private readonly HashSet<SyntaxNode> visitedLocations = new HashSet<SyntaxNode>();
        private readonly HashSet<IParameterSymbol> parameters = new HashSet<IParameterSymbol>(SymbolComparer.Default);

        private ISymbol currentSymbol;
        private SyntaxNode context;
        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;

        private AssignedValueWalker()
        {
        }

        public int Count => this.values.Count;

        public ExpressionSyntax this[int index] => this.values[index];

        public IEnumerator<ExpressionSyntax> GetEnumerator() => this.values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public override void Visit(SyntaxNode node)
        {
            if (this.IsBeforeInScope(node) != Result.Yes)
            {
                return;
            }

            base.Visit(node);
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            this.HandleAssignedValue(this.semanticModel.GetDeclaredSymbolSafe(node, this.cancellationToken), node.Initializer?.Value);
            base.VisitVariableDeclarator(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (node.Initializer != null &&
                SymbolComparer.Equals(this.currentSymbol, this.semanticModel.GetDeclaredSymbolSafe(node, this.cancellationToken)))
            {
                this.values.Add(node.Initializer.Value);
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
            this.HandleAssignedValue(this.semanticModel.GetSymbolSafe(node.Left, this.cancellationToken), node.Right);
            base.VisitAssignmentExpression(node);
        }

        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.LogicalNotExpression:
                    break;
                default:
                    this.HandleAssignedValue(this.semanticModel.GetSymbolSafe(node.Operand, this.cancellationToken), node);
                    break;
            }

            base.VisitPrefixUnaryExpression(node);
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            this.HandleAssignedValue(this.semanticModel.GetSymbolSafe(node.Operand, this.cancellationToken), node);
            base.VisitPostfixUnaryExpression(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            base.VisitInvocationExpression(node);
            if (this.visitedLocations.Add(node))
            {
                var method = this.semanticModel.GetSymbolSafe(node, this.cancellationToken);
                if (method != null)
                {
                    var before = this.values.Count;
                    if (method.ContainingType.Is(this.currentSymbol.ContainingType) ||
                        this.currentSymbol.ContainingType.Is(method.ContainingType))
                    {
                        foreach (var reference in method.DeclaringSyntaxReferences)
                        {
                            this.Visit(reference.GetSyntax(this.cancellationToken));
                        }
                    }

                    if (before != this.values.Count)
                    {
                        for (var i = before; i < this.values.Count; i++)
                        {
                            var parameter = this.semanticModel.GetSymbolSafe(this.values[i], this.cancellationToken) as IParameterSymbol;
                            if (parameter != null)
                            {
                                ExpressionSyntax arg;
                                if (node.TryGetArgumentValue(parameter, this.cancellationToken, out arg))
                                {
                                    this.values[i] = arg;
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void VisitArgument(ArgumentSyntax node)
        {
            if (node.RefOrOutKeyword.IsKind(SyntaxKind.RefKeyword) ||
                node.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword))
            {
                var invocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
                var argSymbol = this.semanticModel.GetSymbolSafe(node.Expression, this.cancellationToken);
                if (invocation != null &&
                    (SymbolComparer.Equals(this.currentSymbol, argSymbol) ||
                     this.parameters.Contains(argSymbol as IParameterSymbol)))
                {
                    var method = this.semanticModel.GetSymbolSafe(invocation, this.cancellationToken);
                    if (method == null ||
                        method.DeclaringSyntaxReferences.Length == 0)
                    {
                        this.values.Add(invocation);
                    }
                    else
                    {
                        foreach (var reference in method.DeclaringSyntaxReferences)
                        {
                            var methodDeclaration = reference.GetSyntax(this.cancellationToken) as MethodDeclarationSyntax;
                            ParameterSyntax parameterSyntax;
                            if (methodDeclaration.TryGetMatchingParameter(node, out parameterSyntax))
                            {
                                var parameter = this.semanticModel.GetDeclaredSymbolSafe(parameterSyntax, this.cancellationToken) as IParameterSymbol;
                                if (parameter == null)
                                {
                                    this.values.Add(invocation);
                                }
                                else
                                {
                                    this.parameters.Add(parameter).IgnoreReturnValue();
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

        internal bool AddAssignedValuesFor(ExpressionSyntax assignedValue)
        {
            var symbol = this.semanticModel.GetSymbolSafe(assignedValue, this.cancellationToken);
            if (symbol == null ||
                !this.visitedLocations.Add(assignedValue))
            {
                return false;
            }

            var before = this.values.Count;
            using (var pooled = Pool.GetOrCreate())
            {
                pooled.Item.currentSymbol = symbol;
                pooled.Item.context = this.context;
                pooled.Item.semanticModel = this.semanticModel;
                pooled.Item.cancellationToken = this.cancellationToken;
                pooled.Item.visitedLocations.UnionWith(this.visitedLocations);

                pooled.Item.Run();
                var parameter = this.currentSymbol as IParameterSymbol;
                if (parameter?.Name == "value")
                {
                    var property = (parameter.ContainingSymbol as IMethodSymbol)?.AssociatedSymbol as IPropertySymbol;
                    if (property != null)
                    {
                        pooled.Item.currentSymbol = property;
                        pooled.Item.Run();
                    }
                }

                foreach (var value in pooled.Item)
                {
                    this.values.Add(value);
                }
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
            if (this.currentSymbol == null)
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
                var type = (INamedTypeSymbol)this.semanticModel.GetDeclaredSymbolSafe(
                        this.context?.FirstAncestorOrSelf<TypeDeclarationSyntax>(),
                        this.cancellationToken);
                if (type == null)
                {
                    return;
                }

                if (this.currentSymbol is IFieldSymbol ||
                    this.currentSymbol is IPropertySymbol)
                {
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

                    foreach (var reference in type.DeclaringSyntaxReferences)
                    {
                        var typeDeclaraion = (TypeDeclarationSyntax)reference.GetSyntax(this.cancellationToken);
                        foreach (var member in typeDeclaraion.Members)
                        {
                            if (member is MethodDeclarationSyntax)
                            {
                                var method = this.semanticModel.GetDeclaredSymbolSafe(member, this.cancellationToken);
                                if (method != null &&
                                    method.DeclaredAccessibility != Accessibility.Private)
                                {
                                    this.Visit(member);
                                }
                            }

                            var propertyDeclaration = member as PropertyDeclarationSyntax;
                            if (propertyDeclaration != null)
                            {
                                var property = (IPropertySymbol)this.semanticModel.GetDeclaredSymbolSafe(member, this.cancellationToken);
                                if (property == null)
                                {
                                    continue;
                                }

                                AccessorDeclarationSyntax accessor;
                                if (property.GetMethod != null &&
                                    property.GetMethod.DeclaredAccessibility != Accessibility.Private)
                                {
                                    if (propertyDeclaration.TryGetGetAccessorDeclaration(out accessor))
                                    {
                                        this.Visit(accessor);
                                    }
                                }

                                if (property.SetMethod != null &&
                                    property.SetMethod.DeclaredAccessibility != Accessibility.Private)
                                {
                                    if (propertyDeclaration.TryGetSetAccessorDeclaration(out accessor))
                                    {
                                        this.Visit(accessor);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    var memnber = this.context?.FirstAncestorOrSelf<MemberDeclarationSyntax>();
                    this.Visit(memnber);
                }
            }

            this.values.PurgeDuplicates();
        }

        private void HandleAssignedValue(ISymbol assignedSymbol, ExpressionSyntax value)
        {
            if (value == null)
            {
                return;
            }

            var property = assignedSymbol as IPropertySymbol;
            if (!SymbolComparer.Equals(this.currentSymbol, property) &&
                (this.currentSymbol is IFieldSymbol || this.currentSymbol is IPropertySymbol) &&
                property != null &&
                Property.AssignsSymbolInSetter(property, this.currentSymbol, this.semanticModel, this.cancellationToken))
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
                    var parameter = this.semanticModel.GetSymbolSafe(this.values[i], this.cancellationToken) as IParameterSymbol;
                    if (Equals(parameter?.ContainingSymbol, property.SetMethod))
                    {
                        this.values[i] = value;
                    }
                }
            }
            else
            {
                if (SymbolComparer.Equals(this.currentSymbol, assignedSymbol) ||
                    this.parameters.Contains(assignedSymbol as IParameterSymbol))
                {
                    this.values.Add(value);
                }
            }
        }

        private Result IsBeforeInScope(SyntaxNode node)
        {
            if (this.context == null ||
                node is BlockSyntax ||
                node.FirstAncestorOrSelf<StatementSyntax>() == null)
            {
                return Result.Yes;
            }

            if (this.currentSymbol is IParameterSymbol ||
                this.currentSymbol is ILocalSymbol)
            {
                if (!this.context.SharesAncestor<MemberDeclarationSyntax>(node))
                {
                    return Result.Yes;
                }

                return node.IsBeforeInScope(this.context);
            }

            if (!this.context.SharesAncestor<ConstructorDeclarationSyntax>(node))
            {
                return Result.Yes;
            }

            return node.IsBeforeInScope(this.context);
        }
    }
}