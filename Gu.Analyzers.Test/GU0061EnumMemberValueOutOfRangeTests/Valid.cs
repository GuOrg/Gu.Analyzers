namespace Gu.Analyzers.Test.GU0061EnumMemberValueOutOfRangeTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Valid
    {
        private static readonly GU0061EnumMemberValueOutOfRange Analyzer = new GU0061EnumMemberValueOutOfRange();

        [Test]
        public static void BitShiftWithinRange()
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
        Good = 1<<30
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DoNotAnalyzeLong()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void StandardEnum()
        {
            var code = @"
namespace N
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
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
