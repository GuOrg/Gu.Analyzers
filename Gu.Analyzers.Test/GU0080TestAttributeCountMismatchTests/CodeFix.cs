namespace Gu.Analyzers.Test.GU0080TestAttributeCountMismatchTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class CodeFix
    {
        private static readonly GU0072AllTypesShouldBeInternal Analyzer = new GU0072AllTypesShouldBeInternal();
        private static readonly MakeInternalFixProvider Fix = new MakeInternalFixProvider();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0072");

        [Test]
        public void InternalTypeCodeFixTest()
        {
            var badCode = @"
namespace RoslynSandbox
{
    public class ↓Hello
    {
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    internal class Hello
    {
    }
}";

            AnalyzerAssert.CodeFix<GU0072AllTypesShouldBeInternal, MakeInternalFixProvider>(badCode, fixedCode);
        }
    }
}
