namespace Gu.Analyzers.Test.Helpers
{
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using NUnit.Framework;

    public partial class AssignedTests
    {
        internal class Symbol
        {
            [TestCase(true)]
            [TestCase(false)]
            public void FieldWithCtorArg(bool recursive)
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
                Assert.AreEqual(true, Assigned.FirstSymbol(field, ctor, recursive, semanticModel, CancellationToken.None, out result));
                Assert.AreEqual("this.value = arg", result?.ToString());
            }

            [TestCase(true)]
            [TestCase(false)]
            public void FieldWithChainedCtorArg(bool recursive)
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
                if (recursive)
                {
                    Assert.AreEqual(true, Assigned.FirstSymbol(field, ctor, true, semanticModel, CancellationToken.None, out result));
                    Assert.AreEqual("this.value = arg", result?.ToString());
                }
                else
                {
                    Assert.AreEqual(false, Assigned.FirstSymbol(field, ctor, false, semanticModel, CancellationToken.None, out result));
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
                var value = syntaxTree.BestMatch<MemberAccessExpressionSyntax>("this.number");
                var ctor = syntaxTree.BestMatch<ConstructorDeclarationSyntax>("Foo(int arg)");
                AssignmentExpressionSyntax result;
                var field = semanticModel.GetSymbolSafe(value, CancellationToken.None);
                if (recursive)
                {
                    Assert.AreEqual(true, Assigned.FirstSymbol(field, ctor, true, semanticModel, CancellationToken.None, out result));
                    Assert.AreEqual("this.number = value", result?.ToString());
                }
                else
                {
                    Assert.AreEqual(false, Assigned.FirstSymbol(field, ctor, false, semanticModel, CancellationToken.None, out result));
                }
            }

            [TestCase(true)]
            [TestCase(false)]
            public void FieldInPropertyExpressionBody(bool recursive)
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
                if (recursive)
                {
                    Assert.AreEqual(true, Assigned.FirstSymbol(field, ctor, true, semanticModel, CancellationToken.None, out result));
                    Assert.AreEqual("this.number = 3", result?.ToString());
                }
                else
                {
                    Assert.AreEqual(false, Assigned.FirstSymbol(field, ctor, false, semanticModel, CancellationToken.None, out result));
                }
            }
        }
    }
}