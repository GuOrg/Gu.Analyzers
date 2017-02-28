namespace Gu.Analyzers
{
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class ArgumentListSyntaxExt
    {
        internal static bool TryGetMatchingArgument(this ArgumentListSyntax arguments, IParameterSymbol parameter, out ArgumentSyntax argument)
        {
            argument = null;
            if (parameter == null ||
                arguments == null ||
                arguments.Arguments.Count == 0)
            {
                return false;
            }

            foreach (var candidate in arguments.Arguments)
            {
                if (candidate.NameColon?.Name?.Identifier.ValueText == parameter.Name)
                {
                    argument = candidate;
                    return true;
                }
            }

            if (arguments.Arguments.Count <= parameter.Ordinal)
            {
                return false;
            }

            argument = arguments.Arguments[parameter.Ordinal];
            return true;
        }

        internal static bool TryGetArgumentValue(this ArgumentListSyntax arguments, IParameterSymbol parameter, CancellationToken cancellationToken, out ExpressionSyntax value)
        {
            value = null;
            if (arguments == null ||
                arguments.Arguments.Count == 0 ||
                parameter == null)
            {
                return false;
            }

            ArgumentSyntax argument;
            if (TryGetMatchingArgument(arguments, parameter, out argument))
            {
                value = argument.Expression;
                return value != null;
            }

            SyntaxNode declaration;
            if (parameter.HasExplicitDefaultValue && parameter.TryGetSingleDeclaration(cancellationToken, out declaration))
            {
                value = (declaration as ParameterSyntax)?.Default.Value;
            }

            return value != null;
        }
    }
}