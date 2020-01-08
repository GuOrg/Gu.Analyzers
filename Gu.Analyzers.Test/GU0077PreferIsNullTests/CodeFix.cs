namespace Gu.Analyzers.Test.GU0077PreferIsNullTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new BinaryExpressionAnalyzer();
        private static readonly CodeFixProvider Fix = new IsNullFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0077PreferIsNull);

        [Test]
        public static void IfNullThrow()
        {
            var before = @"
namespace N
{
    class C
    {
        C(string s)
        {
            if (↓s == null)
            {
                throw new System.ArgumentNullException(nameof(s));
            }
        }
    }
}";

            var after = @"
namespace N
{
    class C
    {
        C(string s)
        {
            if (s is null)
            {
                throw new System.ArgumentNullException(nameof(s));
            }
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }
    }
}
