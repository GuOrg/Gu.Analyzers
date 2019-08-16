namespace Gu.Analyzers.Test.GU0100WrongDocsTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new DocsAnalyzer();
        private static readonly CodeFixProvider Fix = new DocsFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0100WrongDocs);

        [TestCase("string")]
        [TestCase("System.String")]
        [TestCase("Decoder")]
        public static void WhenWrong(string type)
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
}".AssertReplace("string", type);
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
