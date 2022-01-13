namespace Gu.Analyzers.Test.GU0024SealTypeWithDefaultMemberTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class CodeFix
{
    private static readonly ClassDeclarationAnalyzer Analyzer = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0024SealTypeWithDefaultMember);
    private static readonly MakeSealedFix Fix = new();

    [Test]
    public static void Field()
    {
        var before = @"
namespace N
{
    public class ↓C
    {
        public static readonly C Default = new C();
    }
}";

        var after = @"
namespace N
{
    public sealed class C
    {
        public static readonly C Default = new C();
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Make sealed.");
    }

    [Test]
    public static void Property()
    {
        var before = @"
namespace N
{
    public class ↓C
    {
        public static C Default { get; } = new C();
    }
}";

        var after = @"
namespace N
{
    public sealed class C
    {
        public static C Default { get; } = new C();
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Make sealed.");
    }
}
