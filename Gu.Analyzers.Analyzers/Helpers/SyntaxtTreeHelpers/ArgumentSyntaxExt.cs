namespace Gu.Analyzers
{
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class ArgumentSyntaxExt
    {
        internal static bool TryGetSymbol<T>(this ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, out T result)
            where T : class, ISymbol
        {
            result = semanticModel.GetSymbolSafe(argument.Expression, cancellationToken) as T;

            return result != null;
        }

        internal static bool TryGetStringValue(this ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, out string result)
        {
            result = null;
            if (argument?.Expression == null || semanticModel == null)
            {
                return false;
            }

            if (argument.Expression.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return true;
            }

            if (argument.Expression.IsKind(SyntaxKind.StringLiteralExpression) ||
                argument.Expression.IsNameOf())
            {
                var cv = semanticModel.GetConstantValueSafe(argument.Expression, cancellationToken);
                if (cv.HasValue && cv.Value is string)
                {
                    result = (string)cv.Value;
                    return true;
                }
            }

            var symbolInfo = semanticModel.GetSymbolSafe(argument.Expression, cancellationToken);
            if (symbolInfo?.ContainingType?.Name == "String" &&
                symbolInfo.Name == "Empty")
            {
                result = string.Empty;
                return true;
            }

            return false;
        }

        internal static bool TryGetTypeofValue(this ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, out ITypeSymbol result)
        {
            result = null;
            if (argument?.Expression == null || semanticModel == null)
            {
                return false;
            }

            if (argument.Expression is TypeOfExpressionSyntax typeOf)
            {
                var typeSyntax = typeOf.Type;
                var typeInfo = semanticModel.GetTypeInfoSafe(typeSyntax, cancellationToken);
                result = typeInfo.Type;
                return result != null;
            }

            return false;
        }

        internal static bool TryGetNameOf(this ArgumentSyntax argument, out string name)
        {
            name = null;
            if (argument.Expression is InvocationExpressionSyntax invocation &&
                invocation.Expression is IdentifierNameSyntax methodName &&
                invocation.ArgumentList != null &&
                methodName.Identifier.ValueText == "nameof" &&
                invocation.ArgumentList.Arguments.TryFirst(out var nameofArg))
            {
                switch (nameofArg.Expression)
                {
                    case IdentifierNameSyntax identifierName:
                        name = identifierName.Identifier.ValueText;
                        break;
                    case MemberAccessExpressionSyntax memberAccess:
                        name = memberAccess.Name.Identifier.ValueText;
                        break;
                }
            }

            return name != null;
        }

        private static bool IsNameOf(this ExpressionSyntax expression)
        {
            return (expression as InvocationExpressionSyntax)?.IsNameOf() == true;
        }
    }
}