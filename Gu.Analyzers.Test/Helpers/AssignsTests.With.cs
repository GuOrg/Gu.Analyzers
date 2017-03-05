namespace Gu.Analyzers.Test.Helpers
{
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using NUnit.Framework;

    public partial class AssignsTests
    {
        internal class With
        {
            [TestCase(true)]
            [TestCase(false)]
            public void FieldCtorArg(bool recursive)
            {
                var testCode = @"
namespace RoslynSandbox
{
    internal class Foo
    {
        private readonly int value;

        internal Foo(int arg)
        {
            this.value = arg;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.BestMatch<AssignmentExpressionSyntax>("this.value = arg").Right;
                var ctor = syntaxTree.BestMatch<ConstructorDeclarationSyntax>("Foo(int arg)");
                AssignmentExpressionSyntax result;
                var arg = semanticModel.GetSymbolSafe(value, CancellationToken.None);
                Assert.AreEqual(true, Assigns.FirstWith(arg, ctor, recursive, semanticModel, CancellationToken.None, out result));
                Assert.AreEqual("this.value = arg", result?.ToString());
            }

            [TestCase(true)]
            [TestCase(false)]
            public void FieldCtorArgInNested(bool recursive)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    internal class Foo
    {
        private StreamReader reader;

        internal Foo(Stream stream)
        {
            this.reader = new StreamReader(stream);
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.BestMatch<ParameterSyntax>("stream");
                var ctor = syntaxTree.BestMatch<ConstructorDeclarationSyntax>("Foo(Stream stream)");
                AssignmentExpressionSyntax result;
                var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
                Assert.AreEqual(true, Assigns.FirstWith(symbol, ctor, recursive, semanticModel, CancellationToken.None, out result));
                Assert.AreEqual("this.reader = new StreamReader(stream)", result?.ToString());
            }

            [TestCase(true)]
            [TestCase(false)]
            public void ChainedCtorArg(bool recursive)
            {
                var testCode = @"
namespace RoslynSandbox
{
    internal class Foo
    {
        private readonly int value;

        public Foo(int arg)
            : this(arg, 1)
        {
        }

        internal Foo(int chainedArg, int _)
        {
            this.value = chainedArg;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.BestMatch<ParameterSyntax>("arg");
                var ctor = syntaxTree.BestMatch<ConstructorDeclarationSyntax>("Foo(int arg)");
                AssignmentExpressionSyntax result;
                var symbol = semanticModel.GetDeclaredSymbolSafe(value, CancellationToken.None);
                if (recursive)
                {
                    Assert.AreEqual(true, Assigns.FirstWith(symbol, ctor, true, semanticModel, CancellationToken.None, out result));
                    Assert.AreEqual("this.value = chainedArg", result?.ToString());
                }
                else
                {
                    Assert.AreEqual(false, Assigns.FirstWith(symbol, ctor, false, semanticModel, CancellationToken.None, out result));
                }
            }

            [TestCase(true)]
            [TestCase(false)]
            public void FieldWithCtorArgViaProperty(bool recursive)
            {
                var testCode = @"
namespace RoslynSandbox
{
    internal class Foo
    {
        private int number;

        internal Foo(int arg)
        {
            this.Number = arg;
        }

        public int Number
        {
            get { return this.number; }
            set { this.number = value; }
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.BestMatch<ParameterSyntax>("arg");
                var ctor = syntaxTree.BestMatch<ConstructorDeclarationSyntax>("Foo(int arg)");
                AssignmentExpressionSyntax result;
                var symbol = semanticModel.GetDeclaredSymbolSafe(value, CancellationToken.None);
                if (recursive)
                {
                    Assert.AreEqual(true, Assigns.FirstWith(symbol, ctor, true, semanticModel, CancellationToken.None, out result));
                    Assert.AreEqual("this.Number = arg", result?.ToString());
                }
                else
                {
                    Assert.AreEqual(false, Assigns.FirstSymbol(symbol, ctor, false, semanticModel, CancellationToken.None, out result));
                }
            }
        }
    }
}
