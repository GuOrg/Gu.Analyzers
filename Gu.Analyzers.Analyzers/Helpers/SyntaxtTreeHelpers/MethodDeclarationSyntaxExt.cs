namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class MethodDeclarationSyntaxExt
    {
        internal static bool TryGetReturnExpression(this MethodDeclarationSyntax method, out ExpressionSyntax result)
        {
            result = null;
            if (method == null)
            {
                return false;
            }

            if (method.Body != null)
            {
                return method.Body.TryGetReturnExpression(out result);
            }

            if (method.ExpressionBody != null)
            {
                result = method.ExpressionBody.Expression;
                return result != null;
            }

            return false;
        }

    }
}