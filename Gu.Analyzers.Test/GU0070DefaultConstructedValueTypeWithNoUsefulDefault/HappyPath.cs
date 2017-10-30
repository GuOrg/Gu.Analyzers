namespace Gu.Analyzers.Test.GU0070DefaultConstructedValueTypeWithNoUsefulDefault
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly Analyzers.GU0070DefaultConstructedValueTypeWithNoUsefulDefault Analyzer = new Analyzers.GU0070DefaultConstructedValueTypeWithNoUsefulDefault();

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