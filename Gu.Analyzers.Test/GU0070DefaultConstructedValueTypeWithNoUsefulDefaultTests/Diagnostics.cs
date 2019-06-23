namespace Gu.Analyzers.Test.GU0070DefaultConstructedValueTypeWithNoUsefulDefaultTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        private static readonly GU0070DefaultConstructedValueTypeWithNoUsefulDefault Analyzer = new GU0070DefaultConstructedValueTypeWithNoUsefulDefault();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0070");

        [Test]
        public void UselessDefaultGuid()
        {
            var testCode = @"
namespace RoslynSandbox
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void UselessDefaultDateTime()
        {
            var testCode = @"
namespace RoslynSandbox
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}