namespace Gu.Analyzers
{
    using System.Diagnostics.CodeAnalysis;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class ArgumentSyntaxExt
    {
        internal static bool TryGetNameOf(this ArgumentSyntax argument, [NotNullWhen(true)]out string? name)
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
    }
}
