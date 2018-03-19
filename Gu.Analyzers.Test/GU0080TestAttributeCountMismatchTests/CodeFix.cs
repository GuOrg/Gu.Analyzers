namespace Gu.Analyzers.Test.GU0080TestAttributeCountMismatchTests
{
    using System.Collections.Generic;
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class CodeFix
    {
        private static readonly GU0072AllTypesShouldBeInternal Analyzer = new GU0072AllTypesShouldBeInternal();
        private static readonly MakeInternalFixProvider Fix = new MakeInternalFixProvider();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0072AllTypesShouldBeInternal.DiagnosticId);

        [Test]
        public void InternalTypeCodeFixTest()
        {
            var badCode = new List<string>()
            {
                @"
namespace RoslynSandbox
{
    public class ↓Hello
    {
    }
}" };

            var fixedCode = @"
namespace RoslynSandbox
{
    internal class Hello
    {
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, badCode, fixedCode);
        }
    }
}
