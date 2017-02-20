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
                x.symbol = null;
                x.context = null;
                x.semanticModel = null;
                x.cancellationToken = CancellationToken.None;
            });

        private readonly List<ExpressionSyntax> assignedValues = new List<ExpressionSyntax>();

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
                base.VisitAssignmentExpression(node);
            }
        }

        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            var operand = this.semanticModel.GetSymbolSafe(node.Operand, this.cancellationToken);
            if (SymbolComparer.Equals(this.symbol, operand))
            {
                this.assignedValues.Add(node.Operand);
                base.VisitPrefixUnaryExpression(node);
            }
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            var operand = this.semanticModel.GetSymbolSafe(node.Operand, this.cancellationToken);
            if (SymbolComparer.Equals(this.symbol, operand))
            {
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
                    SymbolComparer.Equals(this.symbol, this.semanticModel.GetSymbolSafe(node.Expression, this.cancellationToken)))
                {
                    this.assignedValues.Add(node.Expression);
                }
            }

            base.VisitArgument(node);
        }

        private static Pool<AssignedValueWalker>.Pooled CreateCore(ISymbol symbol, SyntaxNode context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var pooled = Pool.GetOrCreate();
            pooled.Item.Run(symbol, context, semanticModel, cancellationToken);
            return pooled;
        }

        private void Run(ISymbol symbol, SyntaxNode context, SemanticModel semanticModel,
                         CancellationToken cancellationToken)
        {
            this.context = context;
            this.semanticModel = semanticModel;
            this.cancellationToken = cancellationToken;
            this.symbol = symbol;

            foreach (var reference in this.symbol.DeclaringSyntaxReferences)
            {
                var declaration = reference.GetSyntax(cancellationToken);
                this.Visit(declaration);
            }

            var ctor = context?.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
            if (ctor != null)
            {
                this.Visit(ctor);
            }
            else
            {
                var typeDeclaration = context?.FirstAncestorOrSelf<TypeDeclarationSyntax>();
                if (typeDeclaration != null)
                {
                    var type = (INamedTypeSymbol) semanticModel.GetDeclaredSymbolSafe(typeDeclaration, cancellationToken);
                    if (type.Constructors.Length > 0)
                    {
                        foreach (var typeCtor in type.Constructors)
                        {
                            foreach (var reference in typeCtor.DeclaringSyntaxReferences)
                            {
                                throw new NotImplementedException("Check that chained is only checked once");
                                this.Visit(reference.GetSyntax(cancellationToken));
                            }
                        }
                    }
                    else
                    {
                        IMethodSymbol defaultCtor;
                        if (Constructor.TryGetDefault(type.BaseType, out defaultCtor))
                        {
                            foreach (var reference in defaultCtor.DeclaringSyntaxReferences)
                            {
                                this.Visit(reference.GetSyntax(cancellationToken));
                            }
                        }
                    }
                }
                else
                {
                    pooled.Item.Visit(typeDeclaration);
                    foreach (var assignedSymbol in pooled.Item.symbol)
                    {
                        foreach (var reference in assignedSymbol.ContainingSymbol.DeclaringSyntaxReferences)
                        {
                            pooled.Item.Visit(reference.GetSyntax(cancellationToken));
                        }
                    }
                }
            }
        }

        private bool IsBeforeInScope(SyntaxNode node)
        {
            if (this.context == null ||
                node is BlockSyntax ||
                node.FirstAncestorOrSelf<StatementSyntax>() == null ||
                !this.context.SharesAncestor<MemberDeclarationSyntax>(node))
            {
                return true;
            }

            return node.IsBeforeInScope(this.context);
        }
    }
}