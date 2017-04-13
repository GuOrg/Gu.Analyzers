namespace Gu.Analyzers.Test.Helpers
{
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using NUnit.Framework;

    public partial class AssignmentTests
    {
        internal class Symbol
        {
            [TestCase(SearchMode.Recursive)]
            [TestCase(SearchMode.TopLevel)]
            public void FieldWithCtorArg(SearchMode searchMode)
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
                var value = syntaxTree.BestMatch<MemberAccessExpressionSyntax>("this.value");
                var ctor = syntaxTree.BestMatch<ConstructorDeclarationSyntax>("Foo(int arg)");
                AssignmentExpressionSyntax result;
                var field = semanticModel.GetSymbolSafe(value, CancellationToken.None);
                Assert.AreEqual(true, Assignment.FirstForSymbol(field, ctor, searchMode, semanticModel, CancellationToken.None, out result));
                Assert.AreEqual("this.value = arg", result?.ToString());
            }

            [TestCase(SearchMode.Recursive)]
            [TestCase(SearchMode.TopLevel)]
            public void FieldWithChainedCtorArg(SearchMode searchMode)
            {
                var testCode = @"
namespace RoslynSandbox
{
    internal class Foo
    {
        private readonly int value;

        public Foo()
            : this(1)
        {
        }

        internal Foo(int arg)
        {
            this.value = arg;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.BestMatch<MemberAccessExpressionSyntax>("this.value");
                var ctor = syntaxTree.BestMatch<ConstructorDeclarationSyntax>("Foo()");
                AssignmentExpressionSyntax result;
                var field = semanticModel.GetSymbolSafe(value, CancellationToken.None);
                if (searchMode == SearchMode.Recursive)
                {
                    Assert.AreEqual(true, Assignment.FirstForSymbol(field, ctor, SearchMode.Recursive, semanticModel, CancellationToken.None, out result));
                    Assert.AreEqual("this.value = arg", result?.ToString());
                }
                else
                {
                    Assert.AreEqual(false, Assignment.FirstForSymbol(field, ctor, SearchMode.TopLevel, semanticModel, CancellationToken.None, out result));
                }
            }

            [TestCase(SearchMode.Recursive)]
            [TestCase(SearchMode.TopLevel)]
            public void FieldWithCtorArgViaProperty(SearchMode searchMode)
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
                var value = syntaxTree.BestMatch<MemberAccessExpressionSyntax>("this.number");
                var ctor = syntaxTree.BestMatch<ConstructorDeclarationSyntax>("Foo(int arg)");
                AssignmentExpressionSyntax result;
                var field = semanticModel.GetSymbolSafe(value, CancellationToken.None);
                if (searchMode == SearchMode.Recursive)
                {
                    Assert.AreEqual(true, Assignment.FirstForSymbol(field, ctor, SearchMode.Recursive, semanticModel, CancellationToken.None, out result));
                    Assert.AreEqual("this.number = value", result?.ToString());
                }
                else
                {
                    Assert.AreEqual(false, Assignment.FirstForSymbol(field, ctor, SearchMode.TopLevel, semanticModel, CancellationToken.None, out result));
                }
            }

            [TestCase(SearchMode.Recursive)]
            [TestCase(SearchMode.TopLevel)]
            public void FieldInPropertyExpressionBody(SearchMode searchMode)
            {
                var testCode = @"
namespace RoslynSandbox
{
    internal class Foo
    {
        private int number;

        internal Foo()
        {
            var i = this.Number;
        }

        public int Number => this.number = 3;
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.BestMatch<MemberAccessExpressionSyntax>("this.number");
                var ctor = syntaxTree.BestMatch<ConstructorDeclarationSyntax>("Foo()");
                AssignmentExpressionSyntax result;
                var field = semanticModel.GetSymbolSafe(value, CancellationToken.None);
                if (searchMode == SearchMode.Recursive)
                {
                    Assert.AreEqual(true, Assignment.FirstForSymbol(field, ctor, SearchMode.Recursive, semanticModel, CancellationToken.None, out result));
                    Assert.AreEqual("this.number = 3", result?.ToString());
                }
                else
                {
                    Assert.AreEqual(false, Assignment.FirstForSymbol(field, ctor, SearchMode.TopLevel, semanticModel, CancellationToken.None, out result));
                }
            }
        }
    }
}