namespace Gu.Analyzers.Test.Helpers
{
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public class RecursionLoopTests
    {
        [Test]
        public void OneItem()
        {
            var one = SyntaxFactory.IdentifierName("1");
            var two = SyntaxFactory.IdentifierName("2");
            var loop = new RecursionLoop();
            Assert.AreEqual(true, loop.Add(one));
            Assert.AreEqual(false, loop.Add(one));
            Assert.AreEqual(false, loop.Add(one));
            Assert.AreEqual(true, loop.Add(two));
            Assert.AreEqual(false, loop.Add(two));
            Assert.AreEqual(false, loop.Add(two));
        }

        [Test]
        public void TwoItems()
        {
            var one = SyntaxFactory.IdentifierName("1");
            var two = SyntaxFactory.IdentifierName("2");
            var loop = new RecursionLoop();
            Assert.AreEqual(true, loop.Add(one));
            Assert.AreEqual(true, loop.Add(two));
            Assert.AreEqual(true, loop.Add(one));
            Assert.AreEqual(false, loop.Add(two));
            Assert.AreEqual(false, loop.Add(one));
            Assert.AreEqual(false, loop.Add(one));
            Assert.AreEqual(true, loop.Add(two));
            Assert.AreEqual(false, loop.Add(two));
        }

        [Test]
        public void ThreeItems()
        {
            var one = SyntaxFactory.IdentifierName("1");
            var two = SyntaxFactory.IdentifierName("2");
            var three = SyntaxFactory.IdentifierName("3");
            var loop = new RecursionLoop();
            Assert.AreEqual(true, loop.Add(one));
            Assert.AreEqual(false, loop.Add(one));
            Assert.AreEqual(true, loop.Add(two));
            Assert.AreEqual(true, loop.Add(three));
            Assert.AreEqual(true, loop.Add(one));
            Assert.AreEqual(true, loop.Add(two));
            Assert.AreEqual(false, loop.Add(three));
        }
    }
}
