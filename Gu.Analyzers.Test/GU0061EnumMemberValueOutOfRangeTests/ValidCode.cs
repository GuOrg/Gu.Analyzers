namespace Gu.Analyzers.Test.GU0061EnumMemberValueOutOfRangeTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class ValidCode
    {
        private static readonly GU0061EnumMemberValueOutOfRange Analyzer = new GU0061EnumMemberValueOutOfRange();

        [Test]
        public static void BitShiftWithinRange()
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
        Good = 1<<30
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void DoNotAnalyzeLong()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public enum EnumHigh: long
    {
        None = 0,
        A = 1<<1,
        B = 1<<2,
        Bad = 1<<31
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void StandardEnum()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public enum StandardEnum
    {
        None = 0,
        A,
        B,
        C,		
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }
    }
}
