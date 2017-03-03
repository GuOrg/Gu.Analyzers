namespace Gu.Analyzers
{
    using System;
    using System.Collections.Generic;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class MemberPath
    {
        internal static Pool<List<ExpressionSyntax>>.Pooled Create(ExpressionStatementSyntax node)
        {
            throw new NotImplementedException();
            //var pooled = ListPool<ExpressionSyntax>.Create();
            //if (node != null)
            //{
            //    pooled.Item.Visit(node);
            //}

            //return pooled;
        }

        internal static Pool<List<ExpressionSyntax>>.Pooled Create(ExpressionSyntax node)
        {
            ExpressionSyntax member;
            if (!TryFindMember(node, out member))
            {
                return ListPool<ExpressionSyntax>.Create();
            }

            var pooled = ListPool<ExpressionSyntax>.Create();
            do
            {
                switch (member.Kind())
                {
                    case SyntaxKind.ThisExpression:
                    case SyntaxKind.BaseExpression:
                        return pooled;
                    case SyntaxKind.IdentifierName:
                    case SyntaxKind.GenericName:
                        pooled.Item.Add(member);
                        return pooled;
                    case SyntaxKind.SimpleMemberAccessExpression:
                        if ((member as MemberAccessExpressionSyntax)?.Expression.IsEitherKind(SyntaxKind.ThisExpression, SyntaxKind.BaseExpression) == true)
                        {
                            pooled.Item.Add(member);
                            return pooled;
                        }

                        break;
                }

                pooled.Item.Add(member);
            }
            while (TryFindMemberCore(member, out member));

            pooled.Item.Clear();
            return pooled;
        }

        internal static bool TryFindMember(ExpressionSyntax expression, out ExpressionSyntax member)
        {
            member = null;
            if (expression == null)
            {
                return false;
            }

            var invocation = expression as InvocationExpressionSyntax;
            if (invocation != null)
            {
                return TryFindMemberCore(invocation.Expression, out member);
            }

            if (TryPeel(expression, out member))
            {
                if (member is IdentifierNameSyntax)
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