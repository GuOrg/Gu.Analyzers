namespace Gu.Analyzers.Test.CodeFixes.GenerateUselessDocsFixTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static partial class CodeFixWhenMissingParameterDocsSA1611
{
    internal static class SpecialType
    {
        [Test]
        public static void StandardTextForCancellationToken()
        {
            var before = @"
namespace N
{
    using System.Threading;

    public class C
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        public void M(CancellationToken ↓cancellationToken)
        {
        }
    }
}";

            var after = @"
namespace N
{
    using System.Threading;

    public class C
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name=""cancellationToken"">The <see cref=""CancellationToken""/> that cancels the operation.</param>
        public void M(CancellationToken cancellationToken)
        {
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "Generate standard xml documentation for parameter.");
        }
    }
}
