namespace Gu.Analyzers.Test.GU0060EnumMemberValueConflictsWithAnotherTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class Valid
{
    private static readonly GU0060EnumMemberValueConflictsWithAnother Analyzer = new();

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

    [Test]
    public static void ExplicitNoFlags()
    {
        var code = @"
namespace N
{
    public enum E
    {
        None = 0,
        M1 = 1,
        M2 = 2,
        M3 = 3,
        M4 = 4,
        M5 = 5,
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }
}