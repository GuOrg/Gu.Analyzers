namespace Gu.Analyzers.Test.GU0011DontIgnoreReturnValueTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        [TestCase("ints.Select(x => x);")]
        [TestCase("ints.Select(x => x).Where(x => x > 1);")]
        [TestCase("ints.Where(x => x > 1);")]
        public void Linq(string linq)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Linq;
    class Foo
    {
        void Bar()
        {
            var ints = new[] { 1, 2, 3 };
            ↓ints.Select(x => x);
        }
    }
}";
            testCode = testCode.AssertReplace("ints.Select(x => x);", linq);

            var expectedDiagnostic = ExpectedDiagnostic.CreateFromCodeWithErrorsIndicated(
                diagnosticId: "GU0011",
                message: "Don't ignore the returnvalue.",
                code: testCode,
                cleanedSources: out testCode);
            AnalyzerAssert.Diagnostics<GU0011DontIgnoreReturnValue>(expectedDiagnostic, testCode);
        }

        [Test]
        public void StringBuilderWriteLineToString()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Text;
    public class Foo
    {
        private int value;

        public void Bar()
        {
            var sb = new StringBuilder();
            ↓sb.AppendLine(""test"").ToString();
        }
    }
}";
            AnalyzerAssert.Diagnostics<GU0011DontIgnoreReturnValue>(testCode);
        }
    }
}
