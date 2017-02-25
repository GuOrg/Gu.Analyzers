namespace Gu.Analyzers.Test.Helpers
{
    using System.Threading;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NUnit.Framework;

    internal class DisposableTests
    {
        [TestCase("CreateIntStatementBody()", Result.No)]
        [TestCase("CreateIntExpressionBody()", Result.No)]
        [TestCase("CreateIntWithArg()", Result.No)]
        [TestCase("CreateIntId()", Result.No)]
        [TestCase("CreateIntSquare()", Result.No)]
        [TestCase("Id<int>()", Result.No)]
        [TestCase("Id<IDisposable>()", Result.No)]
        [TestCase("File.OpenRead(string.Empty)", Result.Maybe)]
        [TestCase("CreateDisposableStatementBody()", Result.Yes)]
        [TestCase("CreateDisposableExpressionBody()", Result.Yes)]
        [TestCase("CreateDisposableExpressionBodyReturnTypeObject()", Result.Yes)]
        public void IsCreationMethodCall(string code, Result expected)
        {
            var testCode = @"
namespace RoslynSandBox
{
    using System;
    using System.IO;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    internal class Foo
    {
        internal Foo()
        {
            MethodCall();
        }

        internal int CreateIntStatementBody()
        {
            return 1;
        }

        internal int CreateIntExpressionBody() => 2;

        internal int CreateIntWithArg(int arg) => 3;
   
        internal int CreateIntId(int arg) => arg;
   
        internal int CreateIntSquare(int arg) => arg * arg;

        internal IDisposable CreateDisposableStatementBody()
        {
            return new Disposable();
        }

        internal IDisposable CreateDisposableExpressionBody() => new Disposable();
       
        internal object CreateDisposableExpressionBodyReturnTypeObject() => new Disposable();

        internal T Id<T>(T arg) => arg;
    }
}";
            testCode = testCode.AssertReplace("MethodCall()", code);
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.BestMatch<InvocationExpressionSyntax>(code);
            Assert.AreEqual(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
        }
    }
}
