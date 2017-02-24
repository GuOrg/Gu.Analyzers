namespace Gu.Analyzers
{
    using System;
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
                x.assignedValues.Clear();
                x.visitedMembers.Clear();
                x.symbol = null;
                x.context = null;
                x.semanticModel = null;
                x.cancellationToken = CancellationToken.None;
            });

        private readonly List<ExpressionSyntax> assignedValues = new List<ExpressionSyntax>();
        private readonly HashSet<SyntaxNode> visitedMembers = new HashSet<SyntaxNode>();

        private ISymbol symbol;
        private SyntaxNode context;
        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;
        private bool isSamplingRetunValues;

        private AssignedValueWalker()
        {
        }

        public IReadOnlyList<ExpressionSyntax> AssignedValues => this.assignedValues;

        [Obsolete("Remove this, use the overload with expression to capture context for figuring out what ctors to run.")]
        public static Pool<AssignedValueWalker>.Pooled Create(IPropertySymbol property, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return CreateCore(property, null, semanticModel, cancellationToken);
        }

        [Obsolete("Remove this, use the overload with expression to capture context for figuring out what ctors to run.")]
        public static Pool<AssignedValueWalker>.Pooled Create(IFieldSymbol field, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return CreateCore(field, null, semanticModel, cancellationToken);
        }

        public static Pool<AssignedValueWalker>.Pooled Create(ExpressionSyntax value, SemanticModel semanticModel, CancellationToken cancellationToken)
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
        public static Pool<AssignedValueWalker>.Pooled Create(ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
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

        public override void Visit(SyntaxNode node)
        {
            if (!this.IsBeforeInScope(node))
            {
                return;
            }

            if (this.visitedMembers.AddIfNotNull(node as MemberDeclarationSyntax) == false)
            {
                return;
            }

            if (this.visitedMembers.AddIfNotNull(node as AccessorDeclarationSyntax) == false)
            {
                return;
            }

            base.Visit(node);
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            if (node.Initializer != null &&
                SymbolComparer.Equals(this.symbol, this.semanticModel.GetDeclaredSymbolSafe(node, this.cancellationToken)))
            {
                this.assignedValues.Add(node.Initializer.Value);
            }

            base.VisitVariableDeclarator(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (node.Initializer != null &&
                SymbolComparer.Equals(this.symbol, this.semanticModel.GetDeclaredSymbolSafe(node, this.cancellationToken)))
            {
                this.assignedValues.Add(node.Initializer.Value);
            }

            base.VisitPropertyDeclaration(node);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
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
            if (SymbolComparer.Equals(this.symbol, left))
            {
                this.assignedValues.Add(node.Right);
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
            if (SymbolComparer.Equals(this.symbol, operand))
            {
                this.assignedValues.Add(node);
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
            if (SymbolComparer.Equals(this.symbol, operand))
            {
                this.assignedValues.Add(node);
            }
            else if (operand is IPropertySymbol)
            {
                this.VisitSetter(operand);
            }

            base.VisitPostfixUnaryExpression(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (this.symbol is IFieldSymbol ||
                this.symbol is IPropertySymbol)
            {
                var method = this.semanticModel.GetSymbolSafe(node, this.cancellationToken);
                if (method != null)
                {
                    if (method.ContainingType.Is(this.symbol.ContainingType) ||
                        this.symbol.ContainingType.Is(method.ContainingType))
                    {
                        foreach (var reference in method.DeclaringSyntaxReferences)
                        {
                            this.Visit(reference.GetSyntax(this.cancellationToken));
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
                    SymbolComparer.Equals(this.symbol, this.semanticModel.GetSymbolSafe(node.Expression, this.cancellationToken)))
                {
                    this.assignedValues.Add(invocation);
                }
            }

            base.VisitArgument(node);
        }

        public override void VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
        {
            if (this.isSamplingRetunValues)
            {
                this.assignedValues.Add(node.Expression);
            }

            base.VisitArrowExpressionClause(node);
        }

        public override void VisitReturnStatement(ReturnStatementSyntax node)
        {
            if (this.isSamplingRetunValues)
            {
                this.assignedValues.Add(node.Expression);
            }

            base.VisitReturnStatement(node);
        }

        private static Pool<AssignedValueWalker>.Pooled CreateCore(ISymbol symbol, SyntaxNode context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var pooled = Pool.GetOrCreate();
            pooled.Item.symbol = symbol;
            pooled.Item.context = context;
            pooled.Item.semanticModel = semanticModel;
            pooled.Item.cancellationToken = cancellationToken;
            pooled.Item.Run();
            return pooled;
        }

        private void Run()
        {
            if (this.symbol is IFieldSymbol ||
                this.symbol is IPropertySymbol)
            {
                foreach (var reference in this.symbol.DeclaringSyntaxReferences)
                {
                    var memberDeclaration = reference.GetSyntax(this.cancellationToken)?.FirstAncestorOrSelf<MemberDeclarationSyntax>();
                    AccessorDeclarationSyntax getter;
                    if ((memberDeclaration as PropertyDeclarationSyntax).TryGetGetAccessorDeclaration(out getter))
                    {
                        this.isSamplingRetunValues = true;
                        this.Visit(getter);
                        this.isSamplingRetunValues = false;
                        this.visitedMembers.Remove(memberDeclaration).IgnoreReturnValue();
                    }

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
                var typeDeclaration = this.context?.FirstAncestorOrSelf<TypeDeclarationSyntax>();
                if (typeDeclaration == null)
                {
                    return;
                }

                if (this.symbol is IFieldSymbol ||
                    this.symbol is IPropertySymbol)
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

            if (this.symbol is IParameterSymbol ||
                this.symbol is ILocalSymbol)
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
    }
}