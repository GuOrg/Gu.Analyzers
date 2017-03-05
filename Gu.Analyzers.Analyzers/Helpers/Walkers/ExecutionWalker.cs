namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    /// <summary>
    /// Walks code as it is executed.
    /// </summary>
    internal abstract class ExecutionWalker : CSharpSyntaxWalker
    {
        private readonly HashSet<SyntaxNode> visited = new HashSet<SyntaxNode>();

        protected bool recursive;
        protected SemanticModel semanticModel;
        protected CancellationToken cancellationToken;

        public override void Visit(SyntaxNode node)
        {
            if (node is AnonymousFunctionExpressionSyntax)
            {
                return;
            }

            base.Visit(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            base.VisitInvocationExpression(node);
            this.VisitChained(node);
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            base.VisitAssignmentExpression(node);
            if (this.recursive)
            {
                var property = this.semanticModel.GetSymbolSafe(node.Left, this.cancellationToken) as IPropertySymbol;
                if (property?.SetMethod != null &&
                    this.visited.Add(node))
                {
                    foreach (var reference in property.SetMethod.DeclaringSyntaxReferences)
                    {
                        this.Visit(reference.GetSyntax(this.cancellationToken));
                    }
                }
            }
        }

        public override void VisitConstructorInitializer(ConstructorInitializerSyntax node)
        {
            base.VisitConstructorInitializer(node);
            this.VisitChained(node);
        }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            base.VisitObjectCreationExpression(node);
            this.VisitChained(node);
        }

        protected void Clear()
        {
            this.visited.Clear();
            this.recursive = false;
            this.semanticModel = null;
            this.cancellationToken = CancellationToken.None;
        }

        protected void VisitChained(SyntaxNode node)
        {
            if (this.recursive &&
                this.visited.Add(node))
            {
                var method = this.semanticModel.GetSymbolSafe(node, this.cancellationToken);
                foreach (var reference in method.DeclaringSyntaxReferences)
                {
                    this.Visit(reference.GetSyntax(this.cancellationToken));
                }
            }
        }
    }
}