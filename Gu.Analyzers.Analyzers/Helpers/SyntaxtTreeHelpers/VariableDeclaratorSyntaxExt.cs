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

            var symbol = semanticModel.SemanticModelFor(variable).GetDeclaredSymbol(variable, cancellationToken);
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

            var symbol = semanticModel.SemanticModelFor(variable).GetDeclaredSymbol(variable, cancellationToken);
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

        internal static bool IsPassedAsArgument(this VariableDeclaratorSyntax variable, SemanticModel semanticModel, CancellationToken cancellationToken, out InvocationExpressionSyntax invocation)
        {
            invocation = null;
            if (variable == null)
            {
                return false;
            }

            var symbol = semanticModel.SemanticModelFor(variable).GetDeclaredSymbol(variable, cancellationToken);
            if (symbol == null)
            {
                return false;
            }

            var block = variable.FirstAncestorOrSelf<BlockSyntax>();
            if (block?.TryGetInvocation(symbol, out invocation) == true)
            {
                return true;
            }

            return false;
        }
    }
}