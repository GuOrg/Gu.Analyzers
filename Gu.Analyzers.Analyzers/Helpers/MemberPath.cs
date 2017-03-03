namespace Gu.Analyzers
{
    using System;
    using System.Collections.Generic;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class MemberPath
    {
        internal static bool TryFindRootMember(ExpressionSyntax node, out ExpressionSyntax member)
        {
            if (TryPeel(node, out member) &&
                IsRootMember(member))
            {
                return true;
            }

            if (!TryFindMember(node, out member))
            {
                return false;
            }

            do
            {
                if (IsRootMember(member))
                {
                    return true;
                }
            }
            while (TryFindMemberCore(member, out member));

            member = null;
            return false;
        }

        internal static bool IsRootMember(ExpressionSyntax expression)
        {
            ExpressionSyntax member;
            if (!TryPeel(expression, out member))
            {
                return false;
            }

            if (member is IdentifierNameSyntax)
            {
                return true;
            }

            var memberAccess = member as MemberAccessExpressionSyntax;
            if (memberAccess?.Expression != null)
            {
                switch (memberAccess.Expression.Kind())
                {
                    case SyntaxKind.ThisExpression:
                    case SyntaxKind.BaseExpression:
                        return true;
                }
            }

            return false;
        }

        private static bool TryFindMember(ExpressionSyntax expression, out ExpressionSyntax member)
        {
            member = null;
            if (expression == null)
            {
                return false;
            }

            var invocation = expression as InvocationExpressionSyntax;
            if (invocation != null)
            {
                if (invocation.Parent != null &&
                    invocation.Parent.IsKind(SyntaxKind.ConditionalAccessExpression) &&
                    TryPeel(invocation.Parent as ExpressionSyntax, out member))
                {
                    if (IsRootMember(member))
                    {
                        return true;
                    }

                    return TryFindMemberCore(member, out member);
                }

                return TryFindMemberCore(invocation.Expression, out member);
            }

            if (TryPeel(expression, out member))
            {
                if (IsRootMember(member))
                {
                    return true;
                }

                return TryFindMemberCore(member, out member);
            }

            member = null;
            return false;
        }

        private static bool TryFindMemberCore(ExpressionSyntax expression, out ExpressionSyntax member)
        {
            if (!TryPeel(expression, out member))
            {
                return false;
            }

            if (member is IdentifierNameSyntax ||
                member is ThisExpressionSyntax ||
                member is BaseExpressionSyntax)
            {
                member = null;
                return false;
            }

            var memberAccess = expression as MemberAccessExpressionSyntax;
            if (memberAccess != null)
            {
                if (memberAccess.Expression is ThisExpressionSyntax ||
                    memberAccess.Expression is BaseExpressionSyntax)
                {
                    member = null;
                    return false;
                }

                return TryPeel(memberAccess.Expression, out member);
            }

            var memberBinding = expression as MemberBindingExpressionSyntax;
            if (memberBinding != null)
            {
                return TryPeel((expression.Parent?.Parent as ConditionalAccessExpressionSyntax)?.Expression, out member);
            }

            return false;
        }

        private static bool TryPeel(ExpressionSyntax expression, out ExpressionSyntax member)
        {
            member = null;
            if (expression == null)
            {
                return false;
            }

            switch (expression.Kind())
            {
                case SyntaxKind.ThisExpression:
                case SyntaxKind.BaseExpression:
                case SyntaxKind.IdentifierName:
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.MemberBindingExpression:
                    member = expression;
                    return true;
                case SyntaxKind.ParenthesizedExpression:
                    return TryPeel((expression as ParenthesizedExpressionSyntax)?.Expression, out member);
                case SyntaxKind.CastExpression:
                    return TryPeel((expression as CastExpressionSyntax)?.Expression, out member);
                case SyntaxKind.AsExpression:
                    return TryPeel((expression as BinaryExpressionSyntax)?.Left, out member);
                case SyntaxKind.ConditionalAccessExpression:
                    return TryPeel((expression as ConditionalAccessExpressionSyntax)?.Expression, out member);
            }

            return false;
        }
    }
}