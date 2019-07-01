namespace Gu.Analyzers.Test.GU0060EnumMemberValueConflictsWithAnotherTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class ValidCode
    {
        private static readonly GU0060EnumMemberValueConflictsWithAnother Analyzer = new GU0060EnumMemberValueConflictsWithAnother();

        [Test]
        public static void ExplicitAlias()
        {
            var testCode = @"
namespace RoslynSandbox
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ExplicitBitwiseOrSum()
        {
            var testCode = @"
namespace RoslynSandbox
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void SequentialNonFlagEnum()
        {
            var testCode = @"
namespace RoslynSandbox
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void AliasingEnumMembersNonFlag()
        {
            var testCode = @"
namespace RoslynSandbox
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
            RoslynAssert.Valid(Analyzer, testCode);
        }
    }
}
