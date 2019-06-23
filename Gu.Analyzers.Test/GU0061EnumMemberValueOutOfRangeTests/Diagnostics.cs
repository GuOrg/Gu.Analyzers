namespace Gu.Analyzers.Test.GU0061EnumMemberValueOutOfRangeTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        private static readonly GU0061EnumMemberValueOutOfRange Analyzer = new GU0061EnumMemberValueOutOfRange();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0061");

        [Test]
        public void BitShiftOutOfRange()
        {
            var testCode = @"
namespace RoslynSandbox
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
