namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class BlockSyntaxExt
    {
        internal static bool TryGetReturnExpression(this BlockSyntax body, out ExpressionSyntax returnValue)
        {
            using (var pooled = ReturnExpressionsWalker.Create(body))
            {
                if (pooled.Item.ReturnValues.Count > 1)
                {
                    returnValue = null;
                    return false;
                }

                return pooled.Item.ReturnValues.TryGetSingle(out returnValue);
            }
        }

        internal static bool TryGetAssignment(this BlockSyntax body, ISymbol symbol, out AssignmentExpressionSyntax result)
        {
            result = null;
            using (var pooled = AssignmentWalker.Create(body))
            {
                foreach (var assignment in pooled.Item.Assignments)
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
    }
}