namespace Gu.Analyzers.Test.Helpers
{
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;

    using NUnit.Framework;

    internal class ConstructorDeclarationSyntaxExtTests
    {
        [TestCase("Foo()", "Foo()", false)]
        [TestCase("Foo()", "Foo(int value)", true)]
        [TestCase("Foo(int value)", "Foo()", false)]
        [TestCase("Foo()", "Foo(string text)", true)]
        [TestCase("Foo(string text)", "Foo()", false)]
        [TestCase("Foo(int value)", "Foo(string text)", true)]
        [TestCase("Foo(string text)", "Foo(int value)", false)]
        public void ThisChained(string firstSignature, string otherSignature, bool expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
    }

    internal Foo(int value)
        : this()
    {
    }

    internal Foo(string text)
        : this(1)
    {
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var first = syntaxTree.ConstructorDeclarationSyntax(firstSignature);
            var other = syntaxTree.ConstructorDeclarationSyntax(otherSignature);
            Assert.AreEqual(expected, first.IsRunBefore(other, semanticModel, CancellationToken.None));
        }

        [TestCase("Foo()", "Foo()", false)]
        [TestCase("Foo()", "Foo(int value)", false)]
        [TestCase("Foo(int value)", "Foo()", false)]
        [TestCase("Foo()", "Foo(string text)", false)]
        [TestCase("Foo(string text)", "Foo()", false)]
        [TestCase("Foo(int value)", "Foo(string text)", false)]
        [TestCase("Foo(int value)", "Foo(int value)", false)]
        [TestCase("Foo(string text)", "Foo(int value)", false)]
        public void WhenNotChained(string firstSignature, string otherSignature, bool expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
    }

    internal Foo(int value)
    {
    }

    internal Foo(string text)
    {
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var first = syntaxTree.ConstructorDeclarationSyntax(firstSignature);
            var other = syntaxTree.ConstructorDeclarationSyntax(otherSignature);
            Assert.AreEqual(expected, first.IsRunBefore(other, semanticModel, CancellationToken.None));
        }

        [TestCase("Foo()", "FooBase()", false)]
        [TestCase("FooBase()", "Foo()", true)]
        [TestCase("FooBase()", "Foo(int value)", true)]
        [TestCase("FooBase()", "Foo(string text)", true)]
        [TestCase("FooBase(int value)", "Foo()", false)]
        [TestCase("FooBase(int value)", "Foo(int value)", false)]
        [TestCase("FooBase(int value)", "Foo(string text)", false)]
        public void BaseImplicit(string firstSignature, string otherSignature, bool expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase
{
    internal FooBase()
    {
    }

    internal FooBase(int value)
        : this()
    {
    }

    internal FooBase(string text)
        : this(1)
    {
    }
}

internal class Foo : FooBase
{
    internal Foo()
    {
    }

    internal Foo(int value)
        : this()
    {
    }

    internal Foo(string text)
        : this(1)
    {
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var first = syntaxTree.ConstructorDeclarationSyntax(firstSignature);
            var other = syntaxTree.ConstructorDeclarationSyntax(otherSignature);
            Assert.AreEqual(expected, first.IsRunBefore(other, semanticModel, CancellationToken.None));
        }

        [TestCase("Foo()", "FooBase()", false)]
        [TestCase("FooBase()", "Foo()", true)]
        [TestCase("FooBase(int value)", "Foo()", true)]
        [TestCase("FooBase()", "Foo(int value)", true)]
        [TestCase("FooBase(int value)", "Foo(int value)", true)]
        [TestCase("FooBase()", "Foo(string text)", true)]
        [TestCase("FooBase(int value)", "Foo(string text)", true)]
        [TestCase("FooBase(string text)", "Foo()", false)]
        [TestCase("FooBase(string text)", "Foo(int value)", false)]
        [TestCase("FooBase(string text)", "Foo(string text)", false)]
        public void BaseExplicit(string firstSignature, string otherSignature, bool expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase
{
    internal FooBase()
    {
    }

    internal FooBase(int value)
        : this()
    {
    }

    internal FooBase(string text)
        : this(1)
    {
    }
}

internal class Foo : FooBase
{
    internal Foo()
        : base(1)
    {
    }

    internal Foo(int value)
        : this()
    {
    }

    internal Foo(string text)
        : this(1)
    {
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var first = syntaxTree.ConstructorDeclarationSyntax(firstSignature);
            var other = syntaxTree.ConstructorDeclarationSyntax(otherSignature);
            Assert.AreEqual(expected, first.IsRunBefore(other, semanticModel, CancellationToken.None));
        }
    }
}
