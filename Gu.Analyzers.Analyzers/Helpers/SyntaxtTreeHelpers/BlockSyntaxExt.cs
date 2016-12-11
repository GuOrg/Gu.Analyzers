namespace Gu.Analyzers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class BlockSyntaxExt
    {
        internal static bool TryGetReturnExpression(this BlockSyntax body, out ExpressionSyntax returnValue)
        {
            using (var walker = ReturnExpressionsWalker.Create(body))
            {
                if (walker.ReturnValues.Count > 1)
                {
                    returnValue = null;
                    return false;
                }

                return walker.ReturnValues.TryGetSingle(out returnValue);
            }
        }

        internal static bool TryGetAssignment(this BlockSyntax body, ISymbol symbol, out AssignmentExpressionSyntax result)
        {
            result = null;
            using (var walker = AssignmentWalker.Create(body))
            {
                foreach (var assignment in walker.Assignments)
                {
                    if ((assignment.Right as IdentifierNameSyntax)?.Identifier.ValueText == symbol.Name)
                    {
                        result = assignment;
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool TryGetInvocation(this BlockSyntax body, ISymbol symbol, out InvocationExpressionSyntax result)
        {
            result = null;
            using (var walker = InvocationWalker.Create(body))
            {
                foreach (var invocation in walker.Invocations)
                {
                    if (invocation.ArgumentList?.Arguments.Any(a => (a.Expression as IdentifierNameSyntax)?.Identifier.ValueText == symbol.Name) == true)
                    {
                        result = invocation;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}