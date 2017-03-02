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
        public void CreateForDisposePathStatement(string code, string expectedPath)
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
        public void CreateForDisposeInvocation(string code, string expectedPath)
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
            var invocation = syntaxTree.BestMatch<InvocationExpressionSyntax>("Dispose()");
            using (var pooled = MemberPathWalker.Create(invocation))
            {
                Assert.AreEqual(expectedPath, string.Join(", ", pooled.Item));
            }
        }

        [TestCase("this.foo.Get<int>(1)", "foo, Get<int>")]
        [TestCase("this.foo?.Get<int>(1)", "foo, Get<int>")]
        [TestCase("this.foo?.foo.Get<int>(1)", "foo, foo, Get<int>")]
        [TestCase("this.Inner?.Inner.Get<int>(1)", "Inner, Inner, Get<int>")]
        [TestCase("this.Inner?.foo.Get<int>(1)", "Inner, foo, Get<int>")]
        [TestCase("this.Inner?.foo?.Get<int>(1)", "Inner, foo, Get<int>")]
        [TestCase("this.Inner.foo?.Get<int>(1)", "Inner, foo, Get<int>")]
        public void CreateForGenericInvocation(string code, string expectedPath)
        {
            var testCode = @"
namespace RoslynSandBox
{
    using System;

    public sealed class Foo : IDisposable
    {
        private readonly object meh;
        private readonly Foo foo;

        public Foo Inner => this.foo;

        public void Dispose()
        {
            this.foo.Get<int>(1);
        }

        private T Get<T>(int value) => default(T);
    }
 }";
            testCode = testCode.AssertReplace("this.foo.Get<int>(1)", code);
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var invocation = syntaxTree.BestMatch<InvocationExpressionSyntax>("Get<int>(1)");
            using (var pooled = MemberPathWalker.Create(invocation))
            {
                Assert.AreEqual(expectedPath, string.Join(", ", pooled.Item));
            }
        }

        [TestCase("Foo<double>.foo.Get<int>(1)", "Foo<double>, foo, Get<int>")]
        [TestCase("Foo<double>.foo?.Get<int>(1)", "Foo<double>, foo, Get<int>")]
        [TestCase("Foo<double>.foo?.foo.Get<int>(1)", "Foo<double>, foo, foo, Get<int>")]
        [TestCase("Foo<double>.Inner?.Inner.Get<int>(1)", "Foo<double>, Inner, Inner, Get<int>")]
        [TestCase("Foo<double>.Inner?.foo.Get<int>(1)", "Foo<double>, Inner, foo, Get<int>")]
        [TestCase("Foo<double>.Inner?.foo?.Get<int>(1)", "Foo<double>, Inner, foo, Get<int>")]
        [TestCase("Foo<double>.Inner.foo?.Get<int>(1)", "Foo<double>, Inner, foo, Get<int>")]
        public void CreateForStaticGenericInvocation(string code, string expectedPath)
        {
            var testCode = @"
namespace RoslynSandBox
{
    public sealed class Foo<T1>
    {
        private static readonly object meh;
        private static readonly Foo<int> foo;

        public static Foo<int> Inner => foo;

        public static void Bar()
        {
            Foo<double>.foo.Get<int>(1);
        }

        private T2 Get<T2>(int value) => default(T2);
    }
}";
            testCode = testCode.AssertReplace("Foo<double>.foo.Get<int>(1)", code);
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var invocation = syntaxTree.BestMatch<InvocationExpressionSyntax>("Get<int>(1)");
            using (var pooled = MemberPathWalker.Create(invocation))
            {
                Assert.AreEqual(expectedPath, string.Join(", ", pooled.Item));
            }
        }
    }
}