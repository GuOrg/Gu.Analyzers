namespace Gu.Analyzers.Test.GU0010DoNotAssignSameValueTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class Diagnostics
{
    private static readonly SimpleAssignmentAnalyzer Analyzer = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0010DoNotAssignSameValue);

    [TestCase("this.A = this.A;")]
    [TestCase("this.A = A;")]
    [TestCase("A = A;")]
    [TestCase("A = this.A;")]
    public static void AssignToToSelf(string statement)
    {
        var code = @"
namespace N
{
    public class C
    {
        public int A { get; private set; }

        private void M()
        {
            ↓this.A = this.A;
        }
    }
}".AssertReplace("this.A = this.A;", statement);

        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic.WithMessage("Assigning made to same, did you mean to assign something else?"), code);
    }

    [Test]
    public static void SetPropertyToSelfWithThis()
    {
        var code = @"
namespace N
{
    public class C
    {
        public int A { get; private set; }

        private void M()
        {
            ↓this.A = this.A;
        }
    }
}";

        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
    }
}
