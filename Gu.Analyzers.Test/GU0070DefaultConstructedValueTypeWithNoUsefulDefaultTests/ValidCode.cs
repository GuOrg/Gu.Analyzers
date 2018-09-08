namespace Gu.Analyzers.Test.GU0070DefaultConstructedValueTypeWithNoUsefulDefaultTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class ValidCode
    {
        private static readonly GU0070DefaultConstructedValueTypeWithNoUsefulDefault Analyzer = new GU0070DefaultConstructedValueTypeWithNoUsefulDefault();

        [Test]
        public void DefaultValueForGuidCreatedWithDefaultValueExpression()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class A
    {
        public void F()
        {
            var g = default(System.Guid);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void GuidCreatedWithGuidNewGuid()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class A
    {
        public void F()
        {
            var g = System.Guid.NewGuid();
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
