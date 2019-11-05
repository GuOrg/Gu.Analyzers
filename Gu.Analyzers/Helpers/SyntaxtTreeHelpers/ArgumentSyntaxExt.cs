namespace Gu.Analyzers
{
    using System.Diagnostics.CodeAnalysis;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class ArgumentSyntaxExt
    {
        internal static bool TryGetNameOf(this ArgumentSyntax argument, [NotNullWhen(true)] out string? name)
        {
            name = null;
            if (argument.Expression is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier: { ValueText: "nameof" } }, ArgumentList: { } } invocation &&
                invocation.ArgumentList.Arguments.TryFirst(out var nameofArg))
            {
                switch (nameofArg.Expression)
                {
                    case IdentifierNameSyntax identifierName:
                        name = identifierName.Identifier.ValueText;
                        break;
                    case MemberAccessExpressionSyntax { Name: { } memberName }:
                        name = memberName.Identifier.ValueText;
                        break;
                }
            }

            return name != null;
        }
    }
}
