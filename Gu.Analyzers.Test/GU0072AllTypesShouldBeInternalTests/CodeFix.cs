namespace Gu.Analyzers.Test.GU0072AllTypesShouldBeInternalTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class CodeFix
    {
        private static readonly GU0072AllTypesShouldBeInternal Analyzer = new GU0072AllTypesShouldBeInternal();
        private static readonly MakeInternalFix Fix = new MakeInternalFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0072");

        [Test]
        public void Class()
        {
            var testCode = @"
namespace RoslynSandbox
{
    ↓public class Foo
    {
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    internal class Foo
    {
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void Struct()
        {
            var testCode = @"
namespace RoslynSandbox
{
    ↓public struct Foo
    {
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    internal struct Foo
    {
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }
    }
}
