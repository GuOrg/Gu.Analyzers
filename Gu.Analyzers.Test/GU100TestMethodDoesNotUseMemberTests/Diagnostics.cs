namespace Gu.Analyzers.Test.GU100TestMethodDoesNotUseMemberTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
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
        public void METHOD()
        {
            Assert.AreEqual(1,this.value);
        }
    }
}";

            AnalyzerAssert.Diagnostics<GU100TestMethodDoesNotUseMember>(testCode);
        }
    }
}