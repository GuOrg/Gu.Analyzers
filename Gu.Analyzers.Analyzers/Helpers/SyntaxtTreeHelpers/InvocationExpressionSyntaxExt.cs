namespace Gu.Analyzers
{
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class InvocationExpressionSyntaxExt
    {
        internal static bool TryFindInvokee(this InvocationExpressionSyntax invocation, out ExpressionSyntax invokee)
        {
            return TryFindInvokee(invocation.Expression, out invokee);
        }

        internal static string Name(this InvocationExpressionSyntax invocation)
        {
            if (invocation == null)
            {
                return null;
            }

            switch (invocation.Kind())
            {
                case SyntaxKind.InvocationExpression:
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.TypeOfExpression:
                    var identifierName = invocation.Expression as IdentifierNameSyntax;
                    if (identifierName == null)
                    {
                        var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
                        if (memberAccess != null)
                        {
                            identifierName = memberAccess.Name as IdentifierNameSyntax;
                        }
                    }

                    return identifierName?.Identifier.ValueText;
                default:
                    return null;
            }
        }

        internal static bool IsNameOf(this InvocationExpressionSyntax invocation)
        {
            return invocation.Name() == "nameof";
        }

        private static bool TryFindInvokee(ExpressionSyntax expression, out ExpressionSyntax invokee)
        {
            invokee = null;
            var memberAccess = expression as MemberAccessExpressionSyntax;
            if (memberAccess != null)
            {
                var parensExpr = memberAccess.Expression as ParenthesizedExpressionSyntax;
                if (parensExpr != null)
                {
                    return TryFindInvokee(parensExpr.Expression, out invokee);
                }

                if (memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression) &&
                    !(memberAccess.Expression is MemberBindingExpressionSyntax) &&
                    !(memberAccess.Expression is ThisExpressionSyntax))
                {
                    invokee = memberAccess.Expression;
                }
                else
                {
                    invokee = memberAccess;
                }

                return true;
            }

            var parenthesizedExpressionSyntax = expression as ParenthesizedExpressionSyntax;
            if (parenthesizedExpressionSyntax != null)
            {
                return TryFindInvokee(parenthesizedExpressionSyntax.Expression, out invokee);
            }

            var castExpression = expression as CastExpressionSyntax;
            if (castExpression != null)
            {
                return TryFindInvokee(castExpression.Expression, out invokee);
            }

            if (expression.IsKind(SyntaxKind.AsExpression))
            {
                return TryFindInvokee(((BinaryExpressionSyntax)expression).Left, out invokee);
            }

            if (expression is MemberBindingExpressionSyntax)
            {
                var conditionalAccess = expression.Parent.Parent as ConditionalAccessExpressionSyntax;
                if (conditionalAccess != null)
                {
                    return TryFindInvokee(conditionalAccess.Expression, out invokee);
                }
            }

            if (expression is IdentifierNameSyntax)
            {
                invokee = expression;
                return true;
            }

            return false;
        }

        internal static bool TryGetArgumentValue(this InvocationExpressionSyntax invocation, IParameterSymbol parameter, CancellationToken cancellationToken, out ExpressionSyntax value)
        {
            if (invocation?.ArgumentList == null ||
                parameter == null)
            {
                value = null;
                return false;
            }

            return invocation.ArgumentList.TryGetMatchingArgumentValue(parameter, cancellationToken, out value);
        }
    }
}