namespace Gu.Analyzers.Test.Helpers
{
    using System.Linq;
    using System.Threading;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NUnit.Framework;

    internal class ReturnValueWalkerTests
    {
        [TestCase("StaticCreateIntStatementBody()", true, "1")]
        [TestCase("StaticCreateIntStatementBody()", false, "1")]
        [TestCase("StaticCreateIntExpressionBody()", true, "2")]
        [TestCase("StaticCreateIntExpressionBody()", false, "2")]
        [TestCase("IdStatementBody(1)", true, "1")]
        [TestCase("IdStatementBody(1)", false, "1")]
        [TestCase("IdExpressionBody(1)", true, "1")]
        [TestCase("IdExpressionBody(1)", false, "1")]
        [TestCase("AssigningToParameter(1)", true, "1, 2, 3, 4")]
        [TestCase("AssigningToParameter(1)", false, "1, 2, 3, 4")]
        [TestCase("CallingIdExpressionBody(1)", true, "1")]
        [TestCase("CallingIdExpressionBody(1)", false, "IdExpressionBody(arg)")]
        [TestCase("ReturnLocal()", true, "1")]
        [TestCase("ReturnLocal()", false, "1")]
        [TestCase("ReturnLocalAssignedTwice(true)", true, "1, 2, 3")]
        [TestCase("ReturnLocalAssignedTwice(true)", false, "1, 2, 3")]
        [TestCase("Recursive()", true, "")]
        [TestCase("Recursive()", false, "Recursive()")]
        [TestCase("Recursive(1)", true, "")]
        [TestCase("Recursive(1)", false, "Recursive(arg)")]
        [TestCase("Recursive(true)", true, "!flag, true")]
        [TestCase("Recursive(true)", false, "Recursive(!flag), true")]
        public void Call(string code, bool recursive, string expected)
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

        internal static int IdStatementBody(int arg)
        {
            return arg;
        }

        internal static int IdExpressionBody(int arg) => arg;

        internal static int CallingIdExpressionBody(int arg) => IdExpressionBody(arg);

        public static int AssigningToParameter(int arg)
        {
            if (true)
            {
                return arg;
            }
            else
            {
                if (true)
                {
                    arg = 2;
                }
                else
                {
                    arg = 3;
                }

                return arg;
            }

            return 4;
        }

        public static int ConditionalId(int arg)
        {
            if (true)
            {
                return arg;
            }

            return arg;
        }

        public static int ReturnLocal()
        {
            var local = 1;
            return local;
        }

        public static int ReturnLocalAssignedTwice(bool flag)
        {
            var local = 1;
            local = 2;
            if (flag)
            {
                return local;
            }

            local = 5;
            return 3;
        }

        public static int Recursive() => Recursive();

        public static int Recursive(int arg) => Recursive(arg);

        public static int Recursive(bool flag)
        {
            if (flag)
            {
                return Recursive(!flag);
            }

            return flag;
        }
    }
}";
            testCode = testCode.AssertReplace("// Meh()", code);
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.BestMatch<EqualsValueClauseSyntax>(code).Value;
            using (var pooled = ReturnValueWalker.Create(value, recursive, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.Values);
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