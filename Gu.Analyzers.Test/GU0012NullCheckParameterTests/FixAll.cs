namespace Gu.Analyzers.Test.GU0012NullCheckParameterTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static partial class CodeFix
{
    internal static class FixAll
    {
        private static readonly ParameterAnalyzer Analyzer = new();
        private static readonly NullCheckParameterFix Fix = new();
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
