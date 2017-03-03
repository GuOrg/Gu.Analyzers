namespace Gu.Analyzers.Test.Helpers
{
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using NUnit.Framework;

    internal partial class MemberPathTests
    {
        internal class Create
        {

            [TestCase("stream.Dispose()", "stream")]
            [TestCase("this.stream.Dispose()", "this.stream")]
            [TestCase("this.stream?.Dispose()", "this.stream")]
            [TestCase("this.foo.foo.Dispose()", "this.foo.foo, this.foo")]
            [TestCase("this.Inner.foo.Dispose()", "this.Inner.foo, this.Inner")]
            [TestCase("this.foo?.foo.Dispose()", ".foo, this.foo")]
            [TestCase("this.Inner?.foo.Dispose()", ".foo, this.Inner")]
            [TestCase("this.foo?.Inner?.Dispose()", ".Inner, this.foo")]
            [TestCase("this.Inner?.foo?.Dispose()", ".foo, this.Inner")]
            [TestCase("(this.meh as IDisposable)?.Dispose()", "this.meh")]
            [TestCase("((IDisposable)meh).Dispose()", "meh")]
            [TestCase("((IDisposable)meh)?.Dispose()", "meh")]
            [TestCase("((IDisposable)this.meh).Dispose()", "this.meh")]
            [TestCase("((IDisposable)this.meh)?.Dispose()", "this.meh")]
            public void CreateForDisposeInvocation(string code, string expected)
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
                using (var pooled = MemberPath.Create(invocation))
                {
                    Assert.AreEqual(expected, string.Join(", ", pooled.Item));
                }
            }

            [TestCase("foo.Get<int>(1)", "foo")]
            [TestCase("this.foo.Get<int>(1)", "this.foo")]
            [TestCase("this.foo?.Get<int>(1)", "this.foo")]
            [TestCase("this.Inner?.foo.Get<int>(1)", ".foo, this.Inner")]
            [TestCase("this.Inner?.Inner.Get<int>(1)", ".Inner, this.Inner")]
            [TestCase("this.Inner?.foo.Get<int>(1)", ".foo, this.Inner")]
            [TestCase("this.Inner?.foo?.Get<int>(1)", ".foo, this.Inner")]
            [TestCase("this.Inner.foo?.Get<int>(1)", "this.Inner.foo, this.Inner")]
            public void CreateForGenericInvocation(string code, string expected)
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
                using (var pooled = MemberPath.Create(invocation))
                {
                    Assert.AreEqual(expected, string.Join(", ", pooled.Item));
                }
            }

            [TestCase("Foo<double>.foo.Get<int>(1)", "Foo<double>, foo, Get<int>(1)")]
            [TestCase("Foo<double>.foo?.Get<int>(1)", "Foo<double>, foo, Get<int>(1)")]
            [TestCase("Foo<double>.foo?.foo.Get<int>(1)", "Foo<double>, foo, foo, Get<int>(1)")]
            [TestCase("Foo<double>.Inner?.Inner.Get<int>(1)", "Foo<double>, Inner, Inner, Get<int>(1)")]
            [TestCase("Foo<double>.Inner?.foo.Get<int>(1)", "Foo<double>, Inner, foo, Get<int>(1)")]
            [TestCase("Foo<double>.Inner?.foo?.Get<int>(1)", "Foo<double>, Inner, foo, Get<int>(1)")]
            [TestCase("Foo<double>.Inner.foo?.Get<int>(1)", "Foo<double>, Inner, foo, Get<int>(1)")]
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
                using (var pooled = MemberPath.Create(invocation))
                {
                    Assert.AreEqual(expectedPath, string.Join(", ", pooled.Item));
                }
            }

            [TestCase("Task.FromResult(string.Empty)", "")]
            public void Misc(string code, string expectedPath)
            {
                var testCode = @"
namespace RoslynSandBox
{
    using System.Threading.Tasks;

    public sealed class Foo
    {
        public static void Bar()
        {
            Task.FromResult(string.Empty);
        }
    }
}";
                testCode = testCode.AssertReplace("Task.FromResult(string.Empty)", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var invocation = syntaxTree.BestMatch<InvocationExpressionSyntax>(code);
                using (var pooled = MemberPath.Create(invocation))
                {
                    Assert.AreEqual(expectedPath, string.Join(", ", pooled.Item));
                }
            }

            [TestCase("this.First", "")]
            [TestCase("First", "")]
            [TestCase("First.Second", "First")]
            [TestCase("this.First.Second", "this.First")]
            [TestCase("this.First.Second.Third", "this.First.Second, this.First")]
            [TestCase("this.First.Second?.Third", "this.First.Second, this.First")]
            [TestCase("this.First?.Second.Third", "this.First?.Second, this.First")]
            public void CreateForProperty(string code, string expectedPath)
            {
                var testCode = @"
namespace RoslynSandBox
{
    using System;

    public sealed class Foo
    {
        private readonly object meh;
        private readonly Foo foo;

        public Foo First => this.foo;

        public Foo Second => this.foo;

        public Foo Third => this.foo;

        public void Bar()
        {
            var temp = this.Inner;
        }
    }
}";
                testCode = testCode.AssertReplace("this.Inner", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var value = syntaxTree.BestMatch<EqualsValueClauseSyntax>(code)
                                      .Value;
                using (var pooled = MemberPath.Create(value))
                {
                    Assert.AreEqual(expectedPath, string.Join(", ", pooled.Item));
                }
            }
        }
    }
}