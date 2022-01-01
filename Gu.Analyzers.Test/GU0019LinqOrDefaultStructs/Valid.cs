namespace Gu.Analyzers.Test.GU0019LinqOrDefaultStructs;

using Gu.Roslyn.Asserts;

using NUnit.Framework;

internal static class Valid
{
    private static readonly InvocationAnalyzer Analyzer = new();

    [TestCase("IEnumerable<string>", "xs.FirstOrDefault()")]
    [TestCase("IEnumerable<int?>", "xs.FirstOrDefault()")]
    [TestCase("IEnumerable<int>", "xs.First()")]
    public static void Call(string type, string expression)
    {
        var code = @"
namespace N;

using System.Collections.Generic;
using System.Linq;

public class C
{
    public object? M(IEnumerable<string> xs) => xs.FirstOrDefault();
}".AssertReplace("IEnumerable<string>", type)
  .AssertReplace("xs.FirstOrDefault()", expression);
        RoslynAssert.Valid(Analyzer, code);
    }
}
