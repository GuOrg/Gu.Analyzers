namespace Gu.Analyzers.Test.Helpers
{
    using System.Linq;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NUnit.Framework;

    internal partial class AssignedValueWalkerTests
    {
        internal class ReturnValue
        {
            [TestCase("StaticCreateIntStatementBody()", "1")]
            [TestCase("StaticCreateIntExpressionBody()", "2")]
            [TestCase("StaticCreateIntStatementBody(3)", "3")]
            public void Call(string code, string expected)
            {
                var testCode = @"
namespace RoslynSandBox
{
    internal class Foo
    {
        internal Foo()
        {
            var temp = // Meh();
        }

        internal static int StaticCreateIntStatementBody()
        {
            return 1;
        }

        internal static int StaticCreateIntExpressionBody() => 2;

        internal static int StaticCreateIntStatementBody(int arg)
        {
            return arg;
        }
    }
}";
                testCode = testCode.AssertReplace("// Meh()", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.BestMatch<EqualsValueClauseSyntax>(code).Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item.Select(x => x.Value));
                    Assert.AreEqual(expected, actual);
                }
            }
        }
    }
}