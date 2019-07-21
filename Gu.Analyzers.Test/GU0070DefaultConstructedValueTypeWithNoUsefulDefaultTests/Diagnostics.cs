namespace Gu.Analyzers.Test.GU0070DefaultConstructedValueTypeWithNoUsefulDefaultTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Diagnostics
    {
        private static readonly GU0070DefaultConstructedValueTypeWithNoUsefulDefault Analyzer = new GU0070DefaultConstructedValueTypeWithNoUsefulDefault();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0070DefaultConstructedValueTypeWithNoUsefulDefault);

        [Test]
        public static void UselessDefaultGuid()
        {
            var code = @"
namespace N
{
    using System;

    public class A
    {
        public void F()
        {
            var g = ↓new System.Guid();
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void UselessDefaultDateTime()
        {
            var code = @"
namespace N
{
    using System;

    public class A
    {
        public void F()
        {
            var g = ↓new System.DateTime();
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }
    }
}
