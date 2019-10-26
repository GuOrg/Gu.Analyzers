namespace Gu.Analyzers.Test.GU0100WrongDocsTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Valid
    {
        private static readonly DiagnosticAnalyzer Analyzer = new DocsAnalyzer();

        [TestCase("List<int>")]
        [TestCase("StringBuilder")]
        [TestCase("System.Text.StringBuilder")]
        public static void WhenCorrect(string cref)
        {
            var code = @"
namespace N
{
    using System.Text;
    using System.Collections.Generic;

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
}".AssertReplace("StringBuilder", cref);
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenOtherText()
        {
            var code = @"
namespace N
{
    using System.Text;
    using System.Collections.Generic;

    class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        /// <param name=""builder"">For creating a <see cref=""string""/>.</param>
        public void M(StringBuilder builder)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
