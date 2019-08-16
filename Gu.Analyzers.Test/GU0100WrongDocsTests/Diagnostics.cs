namespace Gu.Analyzers.Test.GU0100WrongDocsTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class Diagnostics
    {
        private static readonly DiagnosticAnalyzer Analyzer = new DocsAnalyzer();
        private static readonly CodeFixProvider Fix = new DocsFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0100WrongDocs);

        [Test]
        public static void WhenWrong()
        {
            var before = @"
namespace N
{
    using System.Text;

    class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        /// <param name=""builder"">The <see cref=""↓string""/>.</param>
        public void M(StringBuilder builder)
        {
        }
    }
}";
            var after = @"
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }
    }
}
