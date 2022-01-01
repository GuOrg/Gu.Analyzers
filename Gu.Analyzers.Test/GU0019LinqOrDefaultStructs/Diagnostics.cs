namespace Gu.Analyzers.Test.GU0019LinqOrDefaultStructs;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class Diagnostics
{
    private static readonly InvocationAnalyzer Analyzer = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0019LinqOrDefaultStructs);

    [Test]
    public static void Message()
    {
        var code = @"
namespace N;

using System.Collections.Generic;
using System.Linq;

public class C
{
    public object M(IEnumerable<int> xs) => ↓xs.FirstOrDefault();
}";
        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic.WithMessage("The call never returns null"), code);
    }

    [TestCase("xs.FirstOrDefault()")]
    [TestCase("xs.FirstOrDefault(x => x > 1)")]
    [TestCase("xs.LastOrDefault()")]
    [TestCase("xs.SingleOrDefault()")]
    public static void Call(string expression)
    {
        var code = @"
namespace N;

using System.Collections.Generic;
using System.Linq;

public class C
{
    public object M(IEnumerable<int> xs) => ↓xs.FirstOrDefault();
}".AssertReplace("xs.FirstOrDefault()", expression);
        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
    }
}
