namespace Gu.Analyzers.Test.GU0072AllTypesShouldBeInternalTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly GU0072AllTypesShouldBeInternal Analyzer = new GU0072AllTypesShouldBeInternal();

        [Test]
        public void AllTypesInternal_InternalClass()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    internal class A
    {
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AllTypesInternal_PrivateClass()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    private class A
    {
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AllTypesInternal_ProtectedClass()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    protected class A
    {
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
