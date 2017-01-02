namespace Gu.Analyzers
{
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class VariableDeclaratorSyntaxExt
    {
        internal static bool IsReturned(this VariableDeclaratorSyntax variable, SemanticModel semanticModel, CancellationToken cancellationToken, out ExpressionSyntax returnValue)
        {
            returnValue = null;
            if (variable == null)
            {
                return false;
            }

            var symbol = semanticModel.GetDeclaredSymbolSafe(variable, cancellationToken);
            if (symbol == null)
            {
                return false;
            }

            var block = variable.FirstAncestorOrSelf<BlockSyntax>();
            if (block?.TryGetReturnExpression(out returnValue) == true)
            {
                if ((returnValue as IdentifierNameSyntax)?.Identifier.ValueText == symbol.Name)
                {
                    return true;
                }

                var objectCreation = returnValue as ObjectCreationExpressionSyntax;
                if (objectCreation != null)
                {
                    foreach (var argument in objectCreation.ArgumentList.Arguments)
                    {
                        var arg = semanticModel.GetSymbolSafe(argument.Expression, cancellationToken);
                        if (symbol.Equals(arg))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal static bool IsAssigned(this VariableDeclaratorSyntax variable, SemanticModel semanticModel, CancellationToken cancellationToken, out AssignmentExpressionSyntax assignment)
        {
            assignment = null;
            if (variable == null)
            {
                return false;
            }

            var symbol = semanticModel.GetDeclaredSymbolSafe(variable, cancellationToken);
            if (symbol == null)
            {
                return false;
            }

            var block = variable.FirstAncestorOrSelf<BlockSyntax>();
            if (block?.TryGetAssignment(symbol, out assignment) == true)
            {
                return true;
            }

            return false;
        }
    }
}