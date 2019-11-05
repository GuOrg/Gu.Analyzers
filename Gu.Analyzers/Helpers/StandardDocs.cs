namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class StandardDocs
    {
        private static readonly List<(QualifiedType, string)> Cache = new List<(QualifiedType, string)>
        {
            (KnownSymbol.CancellationToken, "The <see cref=\"CancellationToken\"/> that cancels the operation."),
        };

        internal static bool TryGet(ParameterSyntax parameter, [NotNullWhen(true)] out string? text)
        {
            if (Cache.TryFirst(x => parameter.Type == x.Item1, out var tuple))
            {
                text = tuple.Item2;
                return true;
            }

            text = null;
            return false;
        }
    }
}
