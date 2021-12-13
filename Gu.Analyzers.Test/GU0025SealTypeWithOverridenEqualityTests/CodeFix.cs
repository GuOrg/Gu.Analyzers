namespace Gu.Analyzers.Test.GU0025SealTypeWithOverridenEqualityTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ClassDeclarationAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0025SealTypeWithOverridenEquality);
        private static readonly CodeFixProvider Fix = new MakeSealedFix();

        [Test]
        public static void Field()
        {
            var before = @"
namespace N
{
    public class ↓C
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

            var after = @"
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "Make sealed.");
        }
    }
}
