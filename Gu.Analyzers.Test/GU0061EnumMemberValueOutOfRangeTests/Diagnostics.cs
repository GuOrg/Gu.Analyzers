namespace Gu.Analyzers.Test.GU0061EnumMemberValueOutOfRangeTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Diagnostics
    {
        private static readonly GU0061EnumMemberValueOutOfRange Analyzer = new GU0061EnumMemberValueOutOfRange();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0061EnumMemberValueOutOfRange.DiagnosticId);

        [Test]
        public static void BitShiftOutOfRange()
        {
            var code = @"
namespace N
{
    using System;

    public enum EnumHigh
    {
        None = 0,
        A = 1<<1,
        B = 1<<2,
        Bad = ↓1<<31
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }
    }
}
