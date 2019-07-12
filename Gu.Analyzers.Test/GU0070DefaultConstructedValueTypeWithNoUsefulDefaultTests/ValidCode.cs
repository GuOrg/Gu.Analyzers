namespace Gu.Analyzers.Test.GU0070DefaultConstructedValueTypeWithNoUsefulDefaultTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class ValidCode
    {
        private static readonly GU0070DefaultConstructedValueTypeWithNoUsefulDefault Analyzer = new GU0070DefaultConstructedValueTypeWithNoUsefulDefault();

        [Test]
        public static void DefaultValueForGuidCreatedWithDefaultValueExpression()
        {
            var code = @"
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
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void GuidCreatedWithGuidNewGuid()
        {
            var code = @"
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
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
