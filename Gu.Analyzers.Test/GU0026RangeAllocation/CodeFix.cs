namespace Gu.Analyzers.Test.GU0026RangeAllocation;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class CodeFix
{
    private static readonly RangeAnalyzer Analyzer = new();
    private static readonly UseSpanFix Fix = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0026RangeAllocation);

    [Test]
    public static void Array()
    {
        var before = @"
namespace N;
using System;

public class C
{
    public void M(Int32[] xs)
    {
        var tail = xs↓[1..];
    }
}";

        var after = @"
namespace N;
using System;

public class C
{
    public void M(Int32[] xs)
    {
        var tail = xs.AsSpan()[1..];
    }
}";

        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }
}
