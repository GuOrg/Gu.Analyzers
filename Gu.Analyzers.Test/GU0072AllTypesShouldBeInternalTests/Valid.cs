namespace Gu.Analyzers.Test.GU0072AllTypesShouldBeInternalTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Valid
    {
        private static readonly GU0072AllTypesShouldBeInternal Analyzer = new();

        [TestCase("internal class C")]
        [TestCase("internal struct S")]
        public static void SimpleType(string signature)
        {
            var code = @"
namespace N
{
    using System.Collections.Generic;

    internal class C
    {
    }
}".AssertReplace("internal class C", signature);

            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("internal class C2")]
        [TestCase("protected class C2")]
        [TestCase("private class C2")]
        [TestCase("internal struct S")]
        [TestCase("protected struct S")]
        [TestCase("private struct S")]
        public static void NestedType(string signature)
        {
            var code = @"
namespace N
{
    using System.Collections.Generic;

    internal class C1
    {
        private class C2
        {
        }
    }
}".AssertReplace("private class C2", signature);

            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
