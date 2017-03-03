namespace Gu.Analyzers.Test.Helpers
{
    using System.Linq;
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using NUnit.Framework;

    internal partial class MemberPathTests
    {
        internal class TryFindMember
        {
            [TestCase("this.foo")]
            [TestCase("foo")]
            public void NotFoundForPropertyOrField(string code)
            {
                var testCode = @"
namespace RoslynSandBox
{
    using System;

    public sealed class Foo
    {
        private readonly object meh;
        private readonly Foo foo;

        public Foo Inner => this.foo;

        public void Bar()
        {
            var temp = foo.Inner;
        }

        private T Get<T>(int value) => default(T);
    }
}";
                testCode = testCode.AssertReplace("foo.Inner", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var value = syntaxTree.BestMatch<EqualsValueClauseSyntax>("var temp = ").Value;
                ExpressionSyntax member;
                Assert.AreEqual(false, MemberPath.TryFindMember(value, out member));
                Assert.AreEqual(null, member);
            }

            [TestCase("foo.Inner", "foo")]
            [TestCase("this.foo.Inner", "this.foo")]
            [TestCase("foo.Inner.foo", "foo.Inner")]
            [TestCase("foo.Inner.foo.Inner", "foo.Inner.foo")]
            [TestCase("this.foo.Inner.foo.Inner", "this.foo.Inner.foo")]
            [TestCase("(meh as Foo)?.Inner", "meh")]
            public void ForPropertyOrField(string code, string expected)
            {
                var testCode = @"
namespace RoslynSandBox
{
    using System;

    public sealed class Foo
    {
        private readonly object meh;
        private readonly Foo foo;

        public Foo Inner => this.foo;

        public void Bar()
        {
            var temp = foo.Inner;
        }

        private T Get<T>(int value) => default(T);
    }
}";
                testCode = testCode.AssertReplace("foo.Inner", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var value = syntaxTree.BestMatch<EqualsValueClauseSyntax>("var temp = ").Value;
                ExpressionSyntax member;
                Assert.AreEqual(true, MemberPath.TryFindMember(value, out member));
                Assert.AreEqual(expected, member.ToString());

                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var symbol = semanticModel.GetSymbolSafe(member, CancellationToken.None);
                Assert.AreEqual(expected.Split('.').Last(), symbol.Name);
            }

            [TestCase("this.Get<int>(1)")]
            [TestCase("Get<int>(1)")]
            public void NotFoundForMethodInvocation(string code)
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
                ExpressionSyntax member;
                Assert.AreEqual(false, MemberPath.TryFindMember(invocation, out member));
                Assert.AreEqual(null, member);
            }

            [TestCase("foo.Get<int>(1)", "foo")]
            [TestCase("this.foo.Get<int>(1)", "this.foo")]
            [TestCase("this.foo.Inner.Get<int>(1)", "this.foo.Inner")]
            [TestCase("this.foo.Inner.foo.Get<int>(1)", "this.foo.Inner.foo")]
            [TestCase("foo?.Get<int>(1)", "foo")]
            [TestCase("this.foo?.Get<int>(1)", "this.foo")]
            [TestCase("this.foo?.foo.Get<int>(1)", ".foo")]
            [TestCase("this.Inner?.Inner.Get<int>(1)", ".Inner")]
            [TestCase("this.Inner?.foo.Get<int>(1)", ".foo")]
            [TestCase("this.Inner?.foo?.Get<int>(1)", ".foo")]
            [TestCase("this.Inner.foo?.Get<int>(1)", "this.Inner.foo")]
            [TestCase("((Foo)meh).Get<int>(1)", "meh")]
            [TestCase("((Foo)this.meh).Get<int>(1)", "this.meh")]
            [TestCase("((Foo)this.Inner.meh).Get<int>(1)", "this.Inner.meh")]
            [TestCase("(meh as Foo).Get<int>(1)", "meh")]
            [TestCase("(this.meh as Foo).Get<int>(1)", "this.meh")]
            [TestCase("(this.Inner.meh as Foo).Get<int>(1)", "this.Inner.meh")]
            [TestCase("(this.Inner.meh as Foo)?.Get<int>(1)", "this.Inner.meh")]
            [TestCase("(meh as Foo)?.Get<int>(1)", "meh")]
            public void ForMethodInvocation(string code, string expected)
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
                ExpressionSyntax member;
                Assert.AreEqual(true, MemberPath.TryFindMember(invocation, out member));
                Assert.AreEqual(expected, member.ToString());

                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var symbol = semanticModel.GetSymbolSafe(member, CancellationToken.None);
                Assert.AreEqual(expected.Split('.').Last(), symbol.Name);
            }
        }
    }
}