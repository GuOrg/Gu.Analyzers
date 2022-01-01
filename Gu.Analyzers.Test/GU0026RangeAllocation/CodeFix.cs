namespace Gu.Analyzers.Test.GU0026RangeAllocation;

using Gu.Roslyn.Asserts;

using NUnit.Framework;

internal static class CodeFix
{
    private static readonly RangeAnalyzer Analyzer = new();
    private static readonly UseSpanFix Fix = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0026RangeAllocation);

    [TestCase("int[]")]
    [TestCase("Int32[]")]
    [TestCase("string[]")]
    [TestCase("String[]")]
    public static void AsSpanThenRange(string type)
    {
        var before = @"
namespace N;

using System;

public class C
{
    public void M(int[] xs)
    {
        var tail = xs↓[1..];
    }
}".AssertReplace("int[]", type);

        var after = @"
namespace N;

using System;

public class C
{
    public void M(int[] xs)
    {
        var tail = xs.AsSpan()[1..];
    }
}".AssertReplace("int[]", type);

        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "AsSpan()[1..]");
    }

    [TestCase("int[]")]
    [TestCase("Int32[]")]
    [TestCase("string[]")]
    [TestCase("String[]")]
    public static void AsSpanStart(string type)
    {
        var before = @"
namespace N;

using System;

public class C
{
    public void M(int[] xs)
    {
        var tail = xs↓[1..];
    }
}".AssertReplace("int[]", type);

        var after = @"
namespace N;

using System;

public class C
{
    public void M(int[] xs)
    {
        var tail = xs.AsSpan(1);
    }
}".AssertReplace("int[]", type);

        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "AsSpan(1)");
    }
}
