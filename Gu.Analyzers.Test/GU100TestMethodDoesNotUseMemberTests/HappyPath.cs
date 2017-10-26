namespace Gu.Analyzers.Test.GU100TestMethodDoesNotUseMemberTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly GU100TestMethodDoesNotUseMember Analyzer = new GU100TestMethodDoesNotUseMember();

        [Test]
        public void UsesField()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int value;

        [Test]
        public void Test()
        {
            Assert.AreEqual(1, this.value);
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}