namespace Gu.Analyzers.Test.GU0072AllTypesShouldBeInternalTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly GU0072AllTypesShouldBeInternal Analyzer = new GU0072AllTypesShouldBeInternal();

        [TestCase("internal class Foo")]
        [TestCase("protected class Foo")]
        [TestCase("private class Foo")]
        [TestCase("internal struct Foo")]
        [TestCase("protected struct Foo")]
        [TestCase("private struct Foo")]
        public void SimpleType(string signature)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    internal class Foo
    {
    }
}";
            testCode = testCode.AssertReplace("internal class Foo", signature);
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
