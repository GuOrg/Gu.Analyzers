namespace Gu.Analyzers.Test.GU0060EnumMemberValueConflictsWithAnotherTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Valid
    {
        private static readonly GU0060EnumMemberValueConflictsWithAnother Analyzer = new GU0060EnumMemberValueConflictsWithAnother();

        [Test]
        public static void ExplicitAlias()
        {
            var code = @"
namespace N
{
    using System;

    [Flags]
    public enum Good
    {
        A = 1,
        B = 2,
        Gooooood = B
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ExplicitBitwiseOrSum()
        {
            var code = @"
namespace N
{
    using System;

    [Flags]
    public enum Good
    {
        A = 1,
        B = 2,
        Gooooood = A | B
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void SequentialNonFlagEnum()
        {
            var code = @"
namespace N
{
    using System;

    public enum Bad
    {
        None,
        A,
        B,
        C
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void AliasingEnumMembersNonFlag()
        {
            var code = @"
namespace N
{
    using System;

    public enum Bad
    {
        None,
        A,
        B,
        C = B
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
