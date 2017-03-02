namespace Gu.Analyzers.Test.Helpers
{
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using NUnit.Framework;

    internal class MemberPathWalkerTests
    {
        [TestCase("stream.Dispose()", "stream, Dispose")]
        [TestCase("this.stream.Dispose()", "stream, Dispose")]
        [TestCase("this.stream?.Dispose()", "stream, Dispose")]
        [TestCase("this.foo.foo.Dispose()", "foo, foo, Dispose")]
        [TestCase("this.Inner.foo.Dispose()", "Inner, foo, Dispose")]
        [TestCase("this.foo?.foo.Dispose()", "foo, foo, Dispose")]
        [TestCase("this.Inner?.foo.Dispose()", "Inner, foo, Dispose")]
        [TestCase("this.foo?.foo?.Dispose()", "foo, foo, Dispose")]
        [TestCase("this.Inner?.foo?.Dispose()", "Inner, foo, Dispose")]
        [TestCase("(this.meh as IDisposable)?.Dispose()", "meh, Dispose")]
        [TestCase("((IDisposable)this.meh).Dispose()", "meh, Dispose")]
        [TestCase("((IDisposable)this.meh)?.Dispose()", "meh, Dispose")]
        public void CreateForDisposePath(string code, string expectedPath)
        {
            var testCode = @"
namespace RoslynSandBox
{
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream;
        private readonly object meh;
        private readonly Foo foo;

        public Foo Inner => this.foo;

        public void Dispose()
        {
            this.stream.Dispose();
        }
    }
}";
            testCode = testCode.AssertReplace("this.stream.Dispose()", code);
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var statement = syntaxTree.BestMatch<ExpressionStatementSyntax>(code);
            using (var pooled = MemberPathWalker.Create(statement))
            {
                Assert.AreEqual(expectedPath, string.Join(", ", pooled.Item));
            }
        }
    }
}