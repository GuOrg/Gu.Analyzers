namespace Gu.Analyzers
{
    using System.Collections.Generic;

    using Microsoft.CodeAnalysis;

    internal static class CompilationExt
    {
        internal static IEnumerable<SyntaxTree> AllSyntaxTrees(this Compilation compilation, HashSet<Compilation> @checked = null)
        {
            if (@checked?.Add(compilation) == false)
            {
                yield break;
            }

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                yield return syntaxTree;
            }

            foreach (var metadataReference in compilation.References)
            {
                var compilationReference = metadataReference as CompilationReference;
                if (compilationReference != null)
                {
                    if (@checked == null)
                    {
                        using (var pooled = SetPool<Compilation>.Create())
                        {
                            foreach (var syntaxTree in compilationReference.Compilation.AllSyntaxTrees(pooled.Item))
                            {
                                yield return syntaxTree;
                            }
                        }
                    }
                    else
                    {
                        foreach (var syntaxTree in compilationReference.Compilation.AllSyntaxTrees(@checked))
                        {
                            yield return syntaxTree;
                        }
                    }
                }
            }
        }
    }
}