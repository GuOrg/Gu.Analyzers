namespace Gu.Analyzers.Test.GU0083TestCaseAttributeMismatchMethodTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using NUnit.Framework;

    internal static class NoFix
    {
        private static readonly TestMethodAnalyzer Analyzer = new();
        private static readonly CodeFixProvider Fix = new TestMethodParametersFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0083TestCaseAttributeMismatchMethod);

        [TestCase("[TestCase(\"a\", ↓1, null)]")]
        [TestCase("[TestCase(null, \"a\", ↓1)]")]
        [TestCase("[TestCase(↓1, null, \"b\")]")]
        [TestCase("[TestCase(null, null, ↓1)]")]
        public static void NullArgument(string testCase)
        {
            var testCode = @"
namespace N
{
    using NUnit.Framework;

    public class C
    {
        [TestCase(""x"", ""y"", null)]
        public void M(string x, string y, string z)
        {
        }
    }
}".AssertReplace("[TestCase(\"x\", \"y\", null)]", testCase);

            RoslynAssert.NoFix(Analyzer, Fix, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void ArgumentIsNullAndParameterIsInt()
        {
            var testCode = @"
namespace N
{
    using NUnit.Framework;

    public class C
    {
        [TestCase(↓null)]
        public void M(int obj)
        {
        }
    }
}";
            RoslynAssert.NoFix(Analyzer, Fix, ExpectedDiagnostic, testCode);
        }
    }
}
