namespace Gu.Analyzers.Test.GU0025SealTypeWithOverridenEqualityTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class Valid
{
    private static readonly ClassDeclarationAnalyzer Analyzer = new();

    [Test]
    public static void WhenSealed()
    {
        var code = @"
namespace N
{
    public sealed class C
    {
        public int P { get; }

        public static bool operator ==(C left, C right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(C left, C right)
        {
            return !Equals(left, right);
        }

        public override bool Equals(object? obj) => obj is C other && this.Equals(other);

        public override int GetHashCode() => this.P;

        private bool Equals(C other) => this.P == other.P;
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void WhenNotOverridden()
    {
        var code = @"
namespace N
{
    public class C
    {
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }
}
