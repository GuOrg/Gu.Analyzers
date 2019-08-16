namespace Gu.Analyzers.Test.GU0100WrongDocsTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Valid
    {
        private static readonly DiagnosticAnalyzer Analyzer = new DocsAnalyzer();

        [Test]
        public static void WhenCorrect()
        {
            var code = @"
namespace N
{
    using System.Text;

    class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        /// <param name=""builder"">The <see cref=""StringBuilder""/>.</param>
        public void M(StringBuilder builder)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
