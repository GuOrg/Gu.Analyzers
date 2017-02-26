namespace Gu.Analyzers.Test.Helpers
{
    using System.Linq;
    using System.Threading;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NUnit.Framework;

    internal partial class AssignedValueWalkerTests
    {
        internal class ReturnValue
        {
            [TestCase("StaticCreateIntStatementBody()", "1")]
            [TestCase("StaticCreateIntExpressionBody()", "2")]
            [TestCase("Id1(3)", "3")]
            [TestCase("Id2(3)", "3, 4")]
            [TestCase("Id3(3)", "3, 3")]
            [TestCase("ReturnLocal()", "5")]
            [TestCase("ReturnLocalAssignedTwice(true)", "7, 5, 6")]
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
            var temp1 = temp;
        }

        internal static int StaticCreateIntStatementBody()
        {
            return 1;
        }

        internal static int StaticCreateIntExpressionBody() => 2;

        internal static int Id1(int arg)
        {
            return arg;
        }

        public static int Id2(int arg)
        {
            arg = 4;
            return arg;
        }

        public static int Id3(int arg)
        {
            if (true)
            {
                return arg;
            }

            return arg;
        }

        public static int ReturnLocal()
        {
            var local = 5;
            return local;
        }

        public static int ReturnLocalAssignedTwice(bool flag)
        {
            var local = 5;
            local = 6;
            if (flag)
            {
                return local;
            }

            local = 8;
            return 7;
        }
    }
}";
                testCode = testCode.AssertReplace("// Meh()", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.BestMatch<EqualsValueClauseSyntax>("var temp1 = temp").Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item.Select(x => x.Value));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("Task.Run(() => 1)", "1")]
            [TestCase("Task.Run(() => new Disposable())", "new Disposable()")]
            [TestCase("CreateStringAsync()", "CreateStringAsync()")]
            [TestCase("await CreateStringAsync()", "new string(' ', 1)")]
            [TestCase("await Task.Run(() => new string(' ', 1))", "new string(' ', 1)")]
            [TestCase("await Task.Run(() => new string(' ', 1)).ConfigureAwait(false)", "new string(' ', 1)")]
            [TestCase("await Task.FromResult(new string(' ', 1))", "new string(' ', 1)")]
            public void AsyncAwait(string code, string expected)
            {
                var testCode = @"
namespace RoslynSandBox
{
    using System.Threading.Tasks;

    internal class Foo
    {
        internal async Task Bar()
        {
            var value = // Meh();
        }

        internal static async Task<string> CreateStringAsync()
        {
            await Task.Delay(0);
            return new string(' ', 1);
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
                    Assert.AreEqual(expected, string.Join(", ", pooled.Item.Select(x => x.Value)));
                }
            }
        }
    }
}