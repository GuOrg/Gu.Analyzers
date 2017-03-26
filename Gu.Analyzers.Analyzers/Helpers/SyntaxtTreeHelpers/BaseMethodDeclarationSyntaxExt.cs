namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class BaseMethodDeclarationSyntaxExt
    {
        internal static bool TryGetMatchingParameter(this BaseMethodDeclarationSyntax method, ArgumentSyntax argument, out ParameterSyntax parameter)
        {
            parameter = null;
            if (argument == null ||
                method?.ParameterList == null)
            {
                return false;
            }

            if (argument.NameColon == null)
            {
                var index = argument.FirstAncestorOrSelf<ArgumentListSyntax>()
                                    .Arguments.IndexOf(argument);
                parameter = method.ParameterList.Parameters[index];
                return true;
            }

            foreach (var candidate in method.ParameterList.Parameters)
            {
                if (candidate.Identifier.ValueText == argument.NameColon.Name.Identifier.ValueText)
                {
                    parameter = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}