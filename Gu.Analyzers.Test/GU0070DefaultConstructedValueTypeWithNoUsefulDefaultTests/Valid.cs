namespace Gu.Analyzers.Test.GU0070DefaultConstructedValueTypeWithNoUsefulDefaultTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Valid
    {
        private static readonly GU0070DefaultConstructedValueTypeWithNoUsefulDefault Analyzer = new();

        [TestCase("default(Guid)")]
        [TestCase("Guid.NewGuid()")]
        [TestCase("DateTime.Now")]
        public static void When(string expression)
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public C()
        {
#pragma warning disable CS0219
            var unused = default(Guid);
        }
    }
}".AssertReplace("default(Guid)", expression);

            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
