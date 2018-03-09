namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class NullCheck
    {
        internal static bool IsChecked(IParameterSymbol parameter, SyntaxNode scope, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (parameter == null ||
                scope == null)
            {
                return false;
            }

            using (var walker = NullCheckWalker.Borrow(scope))
            {
                return walker.TryGetFirst(parameter, semanticModel, cancellationToken, out _);
            }
        }

        internal static bool IsCheckedBefore(IParameterSymbol parameter, SyntaxNode scope, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (parameter == null ||
                scope == null)
            {
                return false;
            }

            using (var walker = NullCheckWalker.Borrow(scope.FirstAncestorOrSelf<MemberDeclarationSyntax>()))
            {
                return walker.TryGetFirst(parameter, semanticModel, cancellationToken, out var check) &&
                       check.IsBeforeInScope(scope) == Result.Yes;
            }
        }

        private sealed class NullCheckWalker : PooledWalker<NullCheckWalker>
        {
            private readonly List<BinaryExpressionSyntax> binaryExpressions = new List<BinaryExpressionSyntax>();
            private readonly List<IsPatternExpressionSyntax> isPatterns = new List<IsPatternExpressionSyntax>();
            private readonly List<InvocationExpressionSyntax> invocations = new List<InvocationExpressionSyntax>();

            private NullCheckWalker()
            {
            }

            public static NullCheckWalker Borrow(SyntaxNode scope) => BorrowAndVisit(scope, () => new NullCheckWalker());

            public override void VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                if (node.IsKind(SyntaxKind.EqualsExpression) &&
                    (node.Left.IsKind(SyntaxKind.NullLiteralExpression) ||
                     node.Right.IsKind(SyntaxKind.NullLiteralExpression)))
                {
                    this.binaryExpressions.Add(node);
                }

                if (node.IsKind(SyntaxKind.CoalesceExpression) &&
                    node.Left.IsKind(SyntaxKind.IdentifierName))
                {
                    this.binaryExpressions.Add(node);
                }

                base.VisitBinaryExpression(node);
            }

            public override void VisitIsPatternExpression(IsPatternExpressionSyntax node)
            {
                if (node.Pattern is ConstantPatternSyntax constantPattern &&
                    constantPattern.Expression.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    this.isPatterns.Add(node);
                }

                base.VisitIsPatternExpression(node);
            }

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                if (node.ArgumentList?.Arguments.Count == 2 &&
                    node.ArgumentList.Arguments.TrySingle(x => x.Expression.IsKind(SyntaxKind.NullLiteralExpression), out _) &&
                    node.TryGetMethodName(out var name) &&
                    (name == "Equals" || name == "ReferenceEquals"))
                {
                    this.invocations.Add(node);
                }

                base.VisitInvocationExpression(node);
            }

            public bool TryGetFirst(IParameterSymbol parameter, SemanticModel semanticModel, CancellationToken cancellationToken, out ExpressionSyntax check)
            {
                foreach (var binaryExpression in this.binaryExpressions)
                {
                    if (Is(binaryExpression.Left) ||
                        Is(binaryExpression.Right))
                    {
                        check = binaryExpression;
                        return true;
                    }
                }

                foreach (var isPattern in this.isPatterns)
                {
                    if (Is(isPattern.Expression))
                    {
                        check = isPattern;
                        return true;
                    }
                }

                foreach (var invocation in this.invocations)
                {
                    if (invocation.ArgumentList.Arguments.TryFirst(x => Is(x.Expression), out _))
                    {
                        check = invocation;
                        return true;
                    }
                }

                check = null;
                return false;

                bool Is(ExpressionSyntax expression)
                {
                    return expression is IdentifierNameSyntax identifier &&
                           identifier.Identifier.ValueText == parameter.Name &&
                           semanticModel.GetSymbolSafe(expression, cancellationToken) == parameter;
                }
            }

            protected override void Clear()
            {
                this.binaryExpressions.Clear();
                this.isPatterns.Clear();
                this.invocations.Clear();
            }
        }
    }
}