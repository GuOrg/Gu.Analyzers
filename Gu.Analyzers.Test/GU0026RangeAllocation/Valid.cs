namespace Gu.Analyzers.Test.GU0026RangeAllocation;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class Valid
{
    private static readonly RangeAnalyzer Analyzer = new();

    [Test]
    public static void Span()
    {
        var code = @"
namespace N;

using System;

class C
{
    public ReadOnlySpan<int> M(ReadOnlySpan<int> xs) => xs[1..];
}";
        RoslynAssert.Valid(Analyzer, code);
    }
}
