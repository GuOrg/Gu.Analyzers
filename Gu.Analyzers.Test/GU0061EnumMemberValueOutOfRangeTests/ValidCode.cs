namespace Gu.Analyzers.Test.GU0061EnumMemberValueOutOfRangeTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class ValidCode
    {
        private static readonly GU0061EnumMemberValueOutOfRange Analyzer = new GU0061EnumMemberValueOutOfRange();

        [Test]
        public void BitShiftWithinRange()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public enum EnumHigh
    {
        A = 1<<1,
        B = 1<<2,
        Good = 1<<30
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DoNotAnalyzeLong()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public enum EnumHigh: long
    {
        A = 1<<1,
        B = 1<<2,
        Bad = 1<<31
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void StandardEnum()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public enum StandardEnum
    {
        A,
        B,
        C,		
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
