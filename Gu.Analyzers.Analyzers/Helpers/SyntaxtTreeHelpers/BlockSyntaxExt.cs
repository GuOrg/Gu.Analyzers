namespace Gu.Analyzers
{
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class BlockSyntaxExt
    {
        internal static bool TryGetReturnExpression(this BlockSyntax body, SemanticModel semanticModel, CancellationToken cancellationToken, out ExpressionSyntax returnValue)
        {
            return ReturnValueWalker.TrygetSingle(body, semanticModel, cancellationToken, out returnValue);
        }

        internal static bool TryGetAssignment(this BlockSyntax body, ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken, out AssignmentExpressionSyntax result)
        {
            result = null;
            if (symbol == null)
            {
                return false;
            }

            using (var pooled = AssignmentWalker.Create(body))
            {
                foreach (var assignment in pooled.Item.Assignments)
                {
                    var right = semanticModel.GetSymbolSafe(assignment.Right, cancellationToken);
                    if (symbol.Equals(right))
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