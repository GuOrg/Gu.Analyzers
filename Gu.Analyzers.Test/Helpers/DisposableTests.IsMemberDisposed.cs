namespace Gu.Analyzers.Test.Helpers
{
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using NUnit.Framework;

    internal partial class DisposableTests
    {
        internal class IsMemberDisposed
        {
            [TestCase("stream.Dispose()", "stream", "stream")]
            [TestCase("this.stream.Dispose()", "stream", "stream")]
            [TestCase("this.stream?.Dispose()", "stream", "stream")]
            [TestCase("this.foo.foo.Dispose()", "foo", "foo, foo")]
            [TestCase("this.Inner.foo.Dispose()", "foo", "Inner, foo")]
            [TestCase("this.foo?.foo.Dispose()", "foo", "foo, foo")]
            [TestCase("this.Inner?.foo.Dispose()", "foo", "Inner, foo")]
            [TestCase("this.foo?.foo?.Dispose()", "foo", "foo, foo")]
            [TestCase("this.Inner?.foo?.Dispose()", "foo", "Inner, foo")]
            [TestCase("(this.meh as IDisposable)?.Dispose()", "meh", "meh")]
            [TestCase("((IDisposable)this.meh).Dispose()", "meh", "meh")]
            [TestCase("((IDisposable)this.meh)?.Dispose()", "meh", "meh")]
            public void TryGetDisposed(string code, string expected, string expectedPath)
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
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var statement = syntaxTree.BestMatch<ExpressionStatementSyntax>(code);
                ExpressionSyntax value;
                Assert.AreEqual(true, Disposable.TryGetDisposed(statement, semanticModel, CancellationToken.None, out value));
                Assert.AreEqual(expected, value.ToString());

                using (var pooled = Disposable.GetDisposedPath(statement, semanticModel, CancellationToken.None))
                {
                    Assert.AreEqual(expectedPath, string.Join(", ", pooled.Item));
                }
            }
        }
    }
}