namespace Gu.Analyzers.Test.Helpers
{
    using Microsoft.CodeAnalysis.CSharp;

    using NUnit.Framework;

    internal class SyntaxNodeExtTests
    {
        internal class IsBeforeInScope
        {
            [TestCase("var temp = 1;", "temp = 2;", true)]
            [TestCase("temp = 2;", "var temp = 1;", false)]
            public void SameBlock(string firstStatement, string otherStatement, bool expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        var temp = 1;
        temp = 2;
    }
}");
                var first = syntaxTree.Statement(firstStatement);
                var other = syntaxTree.Statement(otherStatement);
                Assert.AreEqual(expected, first.IsBeforeInScope(other));
            }

            [TestCase("var temp = 1;", "temp = 2;", true)]
            [TestCase("var temp = 1;", "temp = 3;", true)]
            [TestCase("temp = 2;", "var temp = 1;", false)]
            [TestCase("temp = 3;", "var temp = 1;", false)]
            [TestCase("temp = 3;", "temp = 2;", false)]
            [TestCase("temp = 2;", "temp = 3;", false)]
            public void InsideIfBlock(string firstStatement, string otherStatement, bool expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandBox
{
    internal class Foo
    {
        internal Foo()
        {
            var temp = 1;
            if (true)
            {
                temp = 2;
            }
            else
            {
                temp = 3;
            }
        }
    }
}");
                var first = syntaxTree.Statement(firstStatement);
                var other = syntaxTree.Statement(otherStatement);
                Assert.AreEqual(expected, first.IsBeforeInScope(other));
            }

            [TestCase("var temp = 1;", "temp = 2;", true)]
            [TestCase("var temp = 1;", "temp = 3;", true)]
            [TestCase("temp = 2;", "var temp = 1;", false)]
            [TestCase("temp = 3;", "var temp = 1;", false)]
            [TestCase("temp = 3;", "temp = 2;", false)]
            [TestCase("temp = 2;", "temp = 3;", false)]
            public void InsideIfBlockCurlyElse(string firstStatement, string otherStatement, bool expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandBox
{
    internal class Foo
    {
        internal Foo()
        {
            var temp = 1;
            if (true)
                temp = 2;
            else
            {
                temp = 3;
            }
        }
    }
}");
                var first = syntaxTree.Statement(firstStatement);
                var other = syntaxTree.Statement(otherStatement);
                Assert.AreEqual(expected, first.IsBeforeInScope(other));
            }

            [TestCase("var temp = 1;", "temp = 2;", true)]
            [TestCase("var temp = 1;", "temp = 3;", true)]
            [TestCase("var temp = 1;", "temp = 4;", true)]
            [TestCase("temp = 2;", "temp = 4;", true)]
            [TestCase("temp = 3;", "temp = 4;", true)]
            [TestCase("temp = 2;", "var temp = 1;", false)]
            [TestCase("temp = 3;", "var temp = 1;", false)]
            [TestCase("temp = 3;", "temp = 2;", false)]
            [TestCase("temp = 2;", "temp = 3;", false)]
            public void InsideIfBlockNoCurlies(string firstStatement, string otherStatement, bool expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandBox
{
    internal class Foo
    {
        internal Foo()
        {
            var temp = 1;
            if (true)
                temp = 2;
            else
                temp = 3;
            temp = 4;
        }
    }
}");
                var first = syntaxTree.Statement(firstStatement);
                var other = syntaxTree.Statement(otherStatement);
                Assert.AreEqual(expected, first.IsBeforeInScope(other));
            }

            [TestCase("var temp = 1;", "temp = 4;", true)]
            [TestCase("temp = 2;", "temp = 4;", true)]
            [TestCase("temp = 3;", "temp = 4;", true)]
            [TestCase("temp = 4;", "temp = 2;", false)]
            [TestCase("temp = 4;", "temp = 3;", false)]
            public void AfterIfBlock(string firstStatement, string otherStatement, bool expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandBox
{
    internal class Foo
    {
        internal Foo()
        {
            var temp = 1;
            if (true)
            {
                temp = 2;
            }
            else
            {
                temp = 3;
            }

            temp = 4;
        }
    }
}");
                var first = syntaxTree.Statement(firstStatement);
                var other = syntaxTree.Statement(otherStatement);
                Assert.AreEqual(expected, first.IsBeforeInScope(other));
            }
        }
    }
}