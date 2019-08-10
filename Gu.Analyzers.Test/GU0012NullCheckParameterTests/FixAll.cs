namespace Gu.Analyzers.Test.GU0012NullCheckParameterTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static partial class CodeFix
    {
        internal static class FixAll
        {
            private static readonly DiagnosticAnalyzer Analyzer = new ParameterAnalyzer();
            private static readonly CodeFixProvider Fix = new NullCheckParameterFix();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0012NullCheckParameter);

            [Test]
            public static void TwoParameters()
            {
                var before = @"
namespace N
{
    public sealed class C
    {
        public C(string ↓s1, string ↓s2)
        {
        }
    }
}";

                var after = @"
namespace N
{
    public sealed class C
    {
        public C(string s1, string s2)
        {
            if (s1 is null)
            {
                throw new System.ArgumentNullException(nameof(s1));
            }

            if (s2 is null)
            {
                throw new System.ArgumentNullException(nameof(s2));
            }
        }
    }
}";
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }
        }
    }
}
