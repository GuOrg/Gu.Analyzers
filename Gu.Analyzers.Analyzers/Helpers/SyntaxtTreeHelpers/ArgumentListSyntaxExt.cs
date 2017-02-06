namespace Gu.Analyzers
{
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

            if (arguments.Arguments.Count < parameter.Ordinal)
            {
                return false;
            }

            argument = arguments.Arguments[parameter.Ordinal];
            return true;
        }
    }
}