namespace Gu.Analyzers.Test.GU0080TestAttributeCountMismatchTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class CodeFix
{
    private static readonly TestMethodAnalyzer Analyzer = new();
    private static readonly TestMethodParametersFix Fix = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0080TestAttributeCountMismatch);

    [TestCase("string text")]
    [TestCase("string text, int index, bool value")]
    public static void TestAttributeAndParameter(string parameters)
    {
        var before = @"
namespace N
{
    using NUnit.Framework;

    public class C
    {
        [Test]
        public void M↓(string text)
        {
        }
    }
}".AssertReplace("string text", parameters);

        var after = @"
namespace N
{
    using NUnit.Framework;

    public class C
    {
        [Test]
        public void M()
        {
        }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }

    [Test]
    public static void TestCaseAttributeAndParameter()
    {
        var before = @"
namespace N
{
    using NUnit.Framework;

    public class C
    {
        [TestCase(1)]
        public void M↓()
        {
        }
    }
}";

        var after = @"
namespace N
{
    using NUnit.Framework;

    public class C
    {
        [TestCase(1)]
        public void M(int arg0)
        {
        }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }

    [TestCase("int i, int j")]
    [TestCase("string i, int j")]
    [TestCase("string i, int j, int k")]
    [TestCase("string i,string j, int k, int l")]
    public static void TestCaseAttributeAndTooManyParameters(string parameters)
    {
        var before = @"
namespace N
{
    using NUnit.Framework;

    public class C
    {
        [TestCase(1)]
        public void M↓(int i, int j)
        {
        }
    }
}".AssertReplace("int i, int j", parameters);

        var after = @"
namespace N
{
    using NUnit.Framework;

    public class C
    {
        [TestCase(1)]
        public void M(int i)
        {
        }
    }
}";

        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }

    [Test]
    public static void TestCaseParams()
    {
        var code = @"
namespace N
{
    using NUnit.Framework;

    class C
    {
        [TestCase(1)]
        public void M↓(int i, int j, params int[] ints)
        {
        }
    }
}";
        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
    }
}
