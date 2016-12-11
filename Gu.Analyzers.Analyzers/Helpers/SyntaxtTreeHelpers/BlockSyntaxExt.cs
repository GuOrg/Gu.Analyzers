namespace Gu.Analyzers
{
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
    }
}