namespace Gu.Analyzers.Test.Helpers
{
    using System.Threading;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NUnit.Framework;

    internal class ReturnValueWalkerTests
    {
        [TestCase(true, "await Task.SyntaxError(() => new string(' ', 1)).ConfigureAwait(false)")]
        [TestCase(false, "await Task.SyntaxError(() => new string(' ', 1)).ConfigureAwait(false)")]
        public void AwaitSyntaxError(bool recursive, string expected)
        {
            var testCode = @"
using System.Threading.Tasks;

internal class Foo
{
    internal static async Task Bar()
    {
        var text = await CreateAsync().ConfigureAwait(false);
    }

    internal static async Task<string> CreateAsync()
    {
        await Task.Delay(0);
        return await Task.SyntaxError(() => new string(' ', 1)).ConfigureAwait(false);
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.BestMatch<EqualsValueClauseSyntax>("var text = await CreateAsync()").Value;
            using (var pooled = ReturnValueWalker.Create(value, recursive, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("StaticRecursiveExpressionBody", true, "")]
        [TestCase("StaticRecursiveExpressionBody", false, "StaticRecursiveExpressionBody")]
        [TestCase("StaticRecursiveStatementBody)", true, "")]
        [TestCase("StaticRecursiveStatementBody)", false, "StaticRecursiveStatementBody")]
        [TestCase("this.RecursiveExpressionBody", true, "")]
        [TestCase("this.RecursiveExpressionBody", false, "this.RecursiveExpressionBody")]
        [TestCase("this.RecursiveStatementBody)", true, "")]
        [TestCase("this.RecursiveStatementBody)", false, "this.RecursiveStatementBody")]
        [TestCase("this.CalculatedExpressionBody)", true, "1")]
        [TestCase("this.CalculatedExpressionBody)", false, "1")]
        [TestCase("this.CalculatedStatementBody)", true, "1")]
        [TestCase("this.CalculatedStatementBody)", false, "1")]
        [TestCase("this.CalculatedReturningFieldExpressionBody)", true, "this.value")]
        [TestCase("this.CalculatedReturningFieldExpressionBody)", false, "this.value")]
        [TestCase("this.CalculatedReturningFieldStatementBody)", true, "this.value")]
        [TestCase("this.CalculatedReturningFieldStatementBody)", false, "this.value")]
        public void Property(string code, bool recursive, string expected)
        {
            var testCode = @"
namespace RoslynSandBox
{
    internal class Foo
    {
        private readonly int value = 1;

        internal Foo()
        {
            var temp = // Meh();
        }


        public static int StaticRecursiveExpressionBody => StaticRecursiveExpressionBody;

        public static int StaticRecursiveStatementBody
        {
            get
            {
                return StaticRecursiveStatementBody;
            }
        }

        public int RecursiveExpressionBody => this.RecursiveExpressionBody;

        public int RecursiveStatementBody
        {
            get
            {
                return this.RecursiveStatementBody;
            }
        }


        public int CalculatedExpressionBody => 1;

        public int CalculatedStatementBody
        {
            get
            {
                return 1;
            }
        }

        public int CalculatedReturningFieldExpressionBody => this.value;

        public int CalculatedReturningFieldStatementBody
        {
            get
            {
                return this.value;
            }
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
                var actual = string.Join(", ", pooled.Item);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("StaticCreateIntStatementBody()", true, "1")]
        [TestCase("StaticCreateIntStatementBody()", false, "1")]
        [TestCase("StaticCreateIntExpressionBody()", true, "2")]
        [TestCase("StaticCreateIntExpressionBody()", false, "2")]
        [TestCase("IdStatementBody(1)", true, "1")]
        [TestCase("IdStatementBody(1)", false, "1")]
        [TestCase("IdExpressionBody(1)", true, "1")]
        [TestCase("IdExpressionBody(1)", false, "1")]
        [TestCase("OptionalIdExpressionBody()", true, "1")]
        [TestCase("OptionalIdExpressionBody()", false, "1")]
        [TestCase("OptionalIdExpressionBody(1)", true, "1")]
        [TestCase("OptionalIdExpressionBody(1)", false, "1")]
        [TestCase("AssigningToParameter(1)", true, "1, 2, 3, 4")]
        [TestCase("AssigningToParameter(1)", false, "1, 4")]
        [TestCase("CallingIdExpressionBody(1)", true, "1")]
        [TestCase("CallingIdExpressionBody(1)", false, "IdExpressionBody(arg1)")]
        [TestCase("ReturnLocal()", true, "1")]
        [TestCase("ReturnLocal()", false, "local")]
        [TestCase("ReturnLocalAssignedTwice(true)", true, "1, 2, 3")]
        [TestCase("ReturnLocalAssignedTwice(true)", false, "local, 3")]
        [TestCase("Recursive()", true, "")]
        [TestCase("Recursive()", false, "Recursive()")]
        [TestCase("Recursive(1)", true, "")]
        [TestCase("Recursive(1)", false, "Recursive(arg)")]
        [TestCase("Recursive1(1)", true, "")]
        [TestCase("Recursive1(1)", false, "Recursive2(value)")]
        [TestCase("Recursive2(1)", true, "")]
        [TestCase("Recursive2(1)", false, "Recursive1(value)")]
        [TestCase("Recursive(true)", true, "!flag, true")]
        [TestCase("Recursive(true)", false, "Recursive(!flag), true")]
        [TestCase("RecursiveWithOptional(1)", true, "1")]
        [TestCase("RecursiveWithOptional(1)", false, "RecursiveWithOptional(arg, new[] { arg }), 1")]
        [TestCase("RecursiveWithOptional(1, null)", true, "1")]
        [TestCase("RecursiveWithOptional(1, null)", false, "RecursiveWithOptional(arg, new[] { arg }), 1")]
        [TestCase("RecursiveWithOptional(1, new[] { 1, 2 })", true, "1")]
        [TestCase("RecursiveWithOptional(1, new[] { 1, 2 })", false, "RecursiveWithOptional(arg, new[] { arg }), 1")]
        [TestCase("Task.Run(() => 1)", true, "")]
        [TestCase("Task.Run(() => 1)", false, "")]
        public void Call(string code, bool recursive, string expected)
        {
            var testCode = @"
namespace RoslynSandBox
{
    using System;
    using System.Collections.Generic;

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

        internal static int OptionalIdExpressionBody(int arg = 1) => arg;

        internal static int CallingIdExpressionBody(int arg1) => IdExpressionBody(arg1);

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

        public static bool Recursive(bool flag)
        {
            if (flag)
            {
                return Recursive(!flag);
            }

            return flag;
        }

        private static int RecursiveWithOptional(int arg, IEnumerable<int> args = null)
        {
            if (arg == null)
            {
                return RecursiveWithOptional(arg, new[] { arg });
            }

            return arg;
        }

		private static int Recursive1(int value)
        {
            return Recursive2(value);
        }
		
        private static int Recursive2(int value)
        {
            return Recursive1(value);
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
                var actual = string.Join(", ", pooled.Item);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("Func<int> temp = () => 1", true, "1")]
        [TestCase("Func<int> temp = () => 1", false, "1")]
        [TestCase("Func<int, int> temp = x => 1", true, "1")]
        [TestCase("Func<int, int> temp = x => 1", false, "1")]
        [TestCase("Func<int, int> temp = x => x", true, "x")]
        [TestCase("Func<int, int> temp = x => x", false, "x")]
        [TestCase("Func<int> temp = () => { return 1; }", true, "1")]
        [TestCase("Func<int> temp = () => { return 1; }", false, "1")]
        [TestCase("Func<int> temp = () => { if (true) return 1; return 2; }", true, "1, 2")]
        [TestCase("Func<int> temp = () => { if (true) return 1; return 2; }", false, "1, 2")]
        [TestCase("Func<int,int> temp = x => { if (true) return x; return 1; }", true, "x, 1")]
        [TestCase("Func<int,int> temp = x => { if (true) return x; return 1; }", false, "x, 1")]
        [TestCase("Func<int,int> temp = x => { if (true) return 1; return x; }", true, "1, x")]
        [TestCase("Func<int,int> temp = x => { if (true) return 1; return x; }", false, "1, x")]
        [TestCase("Func<int,int> temp = x => { if (true) return 1; return 2; }", true, "1, 2")]
        [TestCase("Func<int,int> temp = x => { if (true) return 1; return 2; }", false, "1, 2")]
        public void Lambda(string code, bool recursive, string expected)
        {
            var testCode = @"
namespace RoslynSandBox
{
    using System;

    internal class Foo
    {
        internal Foo()
        {
            Func<int> temp = () => 1;
        }

        internal static int StaticCreateIntStatementBody()
        {
            return 1;
        }
    }
}";
            testCode = testCode.AssertReplace("Func<int> temp = () => 1", code);
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.BestMatch<EqualsValueClauseSyntax>(code).Value;
            using (var pooled = ReturnValueWalker.Create(value, recursive, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("await Task.Run(() => 1)", true, "1")]
        [TestCase("await Task.Run(() => 1)", false, "1")]
        [TestCase("await Task.Run(() => 1).ConfigureAwait(false)", true, "1")]
        [TestCase("await Task.Run(() => 1).ConfigureAwait(false)", false, "1")]
        [TestCase("await Task.Run(() => new Disposable())", true, "new Disposable()")]
        [TestCase("await Task.Run(() => new Disposable())", false, "new Disposable()")]
        [TestCase("await Task.Run(() => new Disposable()).ConfigureAwait(false)", true, "new Disposable()")]
        [TestCase("await Task.Run(() => new Disposable()).ConfigureAwait(false)", false, "new Disposable()")]
        [TestCase("await Task.Run(() => new string(' ', 1))", true, "new string(' ', 1)")]
        [TestCase("await Task.Run(() => new string(' ', 1))", false, "new string(' ', 1)")]
        [TestCase("await Task.Run(() => new string(' ', 1)).ConfigureAwait(false)", true, "new string(' ', 1)")]
        [TestCase("await Task.Run(() => new string(' ', 1)).ConfigureAwait(false)", false, "new string(' ', 1)")]
        [TestCase("await Task.Run(() => CreateInt())", true, "1")]
        [TestCase("await Task.Run(() => CreateInt())", false, "CreateInt()")]
        [TestCase("await Task.Run(() => CreateInt()).ConfigureAwait(false)", true, "1")]
        [TestCase("await Task.Run(() => CreateInt()).ConfigureAwait(false)", false, "CreateInt()")]
        [TestCase("await Task.FromResult(new string(' ', 1))", true, "new string(' ', 1)")]
        [TestCase("await Task.FromResult(new string(' ', 1))", false, "new string(' ', 1)")]
        [TestCase("await Task.FromResult(new string(' ', 1)).ConfigureAwait(false)", true, "new string(' ', 1)")]
        [TestCase("await Task.FromResult(new string(' ', 1)).ConfigureAwait(false)", false, "new string(' ', 1)")]
        [TestCase("await Task.FromResult(CreateInt())", true, "1")]
        [TestCase("await Task.FromResult(CreateInt())", false, "CreateInt()")]
        [TestCase("await Task.FromResult(CreateInt()).ConfigureAwait(false)", true, "1")]
        [TestCase("await Task.FromResult(CreateInt()).ConfigureAwait(false)", false, "CreateInt()")]
        [TestCase("await CreateAsync(0)", true, "1, 0, 2, 3")]
        [TestCase("await CreateAsync(0)", false, "1, 0, 2, 3")]
        [TestCase("await CreateAsync(0).ConfigureAwait(false)", true, "1, 0, 2, 3")]
        [TestCase("await CreateAsync(0).ConfigureAwait(false)", false, "1, 0, 2, 3")]
        [TestCase("await CreateStringAsync()", true, "new string(' ', 1)")]
        [TestCase("await CreateStringAsync()", false, "new string(' ', 1)")]
        [TestCase("await ReturnAwaitTaskRunAsync()", true, "new string(' ', 1)")]
        [TestCase("await ReturnAwaitTaskRunAsync()", false, "new string(' ', 1)")]
        [TestCase("await RecursiveAsync()", true, "")]
        [TestCase("await RecursiveAsync()", false, "RecursiveAsync()")]
        [TestCase("await RecursiveAsync(1)", true, "")]
        [TestCase("await RecursiveAsync(1)", false, "RecursiveAsync(arg)")]
        public void AsyncAwait(string code, bool recursive, string expected)
        {
            var testCode = @"
namespace RoslynSandBox
{
    using System;
    using System.Threading.Tasks;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    internal class Foo
    {
        internal async Task Bar()
        {
            var value = // Meh();
        }

        internal static async Task<int> RecursiveAsync() => RecursiveAsync();

        internal static async Task<int> RecursiveAsync(int arg) => RecursiveAsync(arg);

        internal static async Task<string> CreateStringAsync()
        {
            await Task.Delay(0);
            return new string(' ', 1);
        }

        internal static async Task<string> ReturnAwaitTaskRunAsync()
        {
            await Task.Delay(0);
            return await Task.Run(() => new string(' ', 1)).ConfigureAwait(false);
        }

        internal static Task<int> CreateAsync(int arg)
        {
            switch (arg)
            {
                case 0:
                    return Task.FromResult(1);
                case 1:
                    return Task.FromResult(arg);
                case 2:
                    return Task.Run(() => 2);
                case 3:
                    return Task.Run(() => arg);
                case 4:
                    return Task.Run(() => { return 3; });
                default:
                    return Task.Run(() => { return arg; });
            }
        }

        internal static async int CreateInt() => 1;
    }
}";
            testCode = testCode.AssertReplace("// Meh()", code);
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.BestMatch<EqualsValueClauseSyntax>(code).Value;
            using (var pooled = ReturnValueWalker.Create(value, recursive, semanticModel, CancellationToken.None))
            {
                Assert.AreEqual(expected, string.Join(", ", pooled.Item));
            }
        }
    }
}