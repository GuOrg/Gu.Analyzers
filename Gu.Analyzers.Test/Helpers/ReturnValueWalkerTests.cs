namespace Gu.Analyzers.Test.Helpers
{
    using System.Threading;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NUnit.Framework;

    internal class ReturnValueWalkerTests
    {
        [TestCase(SearchMode.Recursive, "Task.SyntaxError(() => new string(' ', 1)).ConfigureAwait(false)")]
        [TestCase(SearchMode.TopLevel, "await Task.SyntaxError(() => new string(' ', 1)).ConfigureAwait(false)")]
        public void AwaitSyntaxError(SearchMode searchMode, string expected)
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
            using (var pooled = ReturnValueWalker.Create(value, searchMode, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("StaticRecursiveExpressionBody", SearchMode.Recursive, "")]
        [TestCase("StaticRecursiveExpressionBody", SearchMode.TopLevel, "StaticRecursiveExpressionBody")]
        [TestCase("StaticRecursiveStatementBody", SearchMode.Recursive, "")]
        [TestCase("StaticRecursiveStatementBody", SearchMode.TopLevel, "StaticRecursiveStatementBody")]
        [TestCase("this.RecursiveExpressionBody", SearchMode.Recursive, "")]
        [TestCase("this.RecursiveExpressionBody", SearchMode.TopLevel, "this.RecursiveExpressionBody")]
        [TestCase("this.RecursiveStatementBody", SearchMode.Recursive, "")]
        [TestCase("this.RecursiveStatementBody", SearchMode.TopLevel, "this.RecursiveStatementBody")]
        [TestCase("this.CalculatedExpressionBody", SearchMode.Recursive, "1")]
        [TestCase("this.CalculatedExpressionBody", SearchMode.TopLevel, "1")]
        [TestCase("this.CalculatedStatementBody", SearchMode.Recursive, "1")]
        [TestCase("this.CalculatedStatementBody", SearchMode.TopLevel, "1")]
        [TestCase("this.ThisExpressionBody", SearchMode.Recursive, "this")]
        [TestCase("this.ThisExpressionBody", SearchMode.TopLevel, "this")]
        [TestCase("this.CalculatedReturningFieldExpressionBody", SearchMode.Recursive, "this.value")]
        [TestCase("this.CalculatedReturningFieldExpressionBody", SearchMode.TopLevel, "this.value")]
        [TestCase("this.CalculatedReturningFieldStatementBody", SearchMode.Recursive, "this.value")]
        [TestCase("this.CalculatedReturningFieldStatementBody", SearchMode.TopLevel, "this.value")]
        public void Property(string code, SearchMode searchMode, string expected)
        {
            var testCode = @"
namespace RoslynSandbox
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

        public Foo ThisExpressionBody => this;

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
            using (var pooled = ReturnValueWalker.Create(value, searchMode, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("StaticCreateIntStatementBody()", SearchMode.Recursive, "1")]
        [TestCase("StaticCreateIntStatementBody()", SearchMode.TopLevel, "1")]
        [TestCase("StaticCreateIntExpressionBody()", SearchMode.Recursive, "2")]
        [TestCase("StaticCreateIntExpressionBody()", SearchMode.TopLevel, "2")]
        [TestCase("IdStatementBody(1)", SearchMode.Recursive, "1")]
        [TestCase("IdStatementBody(1)", SearchMode.TopLevel, "1")]
        [TestCase("IdExpressionBody(1)", SearchMode.Recursive, "1")]
        [TestCase("IdExpressionBody(1)", SearchMode.TopLevel, "1")]
        [TestCase("OptionalIdExpressionBody()", SearchMode.Recursive, "1")]
        [TestCase("OptionalIdExpressionBody()", SearchMode.TopLevel, "1")]
        [TestCase("OptionalIdExpressionBody(1)", SearchMode.Recursive, "1")]
        [TestCase("OptionalIdExpressionBody(1)", SearchMode.TopLevel, "1")]
        [TestCase("AssigningToParameter(1)", SearchMode.Recursive, "1, 2, 3, 4")]
        [TestCase("AssigningToParameter(1)", SearchMode.TopLevel, "1, 4")]
        [TestCase("CallingIdExpressionBody(1)", SearchMode.Recursive, "1")]
        [TestCase("CallingIdExpressionBody(1)", SearchMode.TopLevel, "IdExpressionBody(arg1)")]
        [TestCase("ReturnLocal()", SearchMode.Recursive, "1")]
        [TestCase("ReturnLocal()", SearchMode.TopLevel, "local")]
        [TestCase("ReturnLocalAssignedTwice(true)", SearchMode.Recursive, "1, 2, 3")]
        [TestCase("ReturnLocalAssignedTwice(true)", SearchMode.TopLevel, "local, 3")]
        [TestCase("Recursive()", SearchMode.Recursive, "")]
        [TestCase("Recursive()", SearchMode.TopLevel, "Recursive()")]
        [TestCase("Recursive(1)", SearchMode.Recursive, "")]
        [TestCase("Recursive(1)", SearchMode.TopLevel, "Recursive(arg)")]
        [TestCase("Recursive1(1)", SearchMode.Recursive, "")]
        [TestCase("Recursive1(1)", SearchMode.TopLevel, "Recursive2(value)")]
        [TestCase("Recursive2(1)", SearchMode.Recursive, "")]
        [TestCase("Recursive2(1)", SearchMode.TopLevel, "Recursive1(value)")]
        [TestCase("Recursive(true)", SearchMode.Recursive, "!flag, true")]
        [TestCase("Recursive(true)", SearchMode.TopLevel, "Recursive(!flag), true")]
        [TestCase("RecursiveWithOptional(1)", SearchMode.Recursive, "1")]
        [TestCase("RecursiveWithOptional(1)", SearchMode.TopLevel, "RecursiveWithOptional(arg, new[] { arg }), 1")]
        [TestCase("RecursiveWithOptional(1, null)", SearchMode.Recursive, "1")]
        [TestCase("RecursiveWithOptional(1, null)", SearchMode.TopLevel, "RecursiveWithOptional(arg, new[] { arg }), 1")]
        [TestCase("RecursiveWithOptional(1, new[] { 1, 2 })", SearchMode.Recursive, "1")]
        [TestCase("RecursiveWithOptional(1, new[] { 1, 2 })", SearchMode.TopLevel, "RecursiveWithOptional(arg, new[] { arg }), 1")]
        [TestCase("Task.Run(() => 1)", SearchMode.Recursive, "")]
        [TestCase("Task.Run(() => 1)", SearchMode.TopLevel, "")]
        [TestCase("this.ThisExpressionBody()", SearchMode.Recursive, "this")]
        [TestCase("this.ThisExpressionBody()", SearchMode.TopLevel, "this")]
        public void Call(string code, SearchMode searchMode, string expected)
        {
            var testCode = @"
namespace RoslynSandbox
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

        public Foo ThisExpressionBody() => this;

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
            using (var pooled = ReturnValueWalker.Create(value, searchMode, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("Func<int> temp = () => 1", SearchMode.Recursive, "1")]
        [TestCase("Func<int> temp = () => 1", SearchMode.TopLevel, "1")]
        [TestCase("Func<int, int> temp = x => 1", SearchMode.Recursive, "1")]
        [TestCase("Func<int, int> temp = x => 1", SearchMode.TopLevel, "1")]
        [TestCase("Func<int, int> temp = x => x", SearchMode.Recursive, "x")]
        [TestCase("Func<int, int> temp = x => x", SearchMode.TopLevel, "x")]
        [TestCase("Func<int> temp = () => { return 1; }", SearchMode.Recursive, "1")]
        [TestCase("Func<int> temp = () => { return 1; }", SearchMode.TopLevel, "1")]
        [TestCase("Func<int> temp = () => { if (true) return 1; return 2; }", SearchMode.Recursive, "1, 2")]
        [TestCase("Func<int> temp = () => { if (true) return 1; return 2; }", SearchMode.TopLevel, "1, 2")]
        [TestCase("Func<int,int> temp = x => { if (true) return x; return 1; }", SearchMode.Recursive, "x, 1")]
        [TestCase("Func<int,int> temp = x => { if (true) return x; return 1; }", SearchMode.TopLevel, "x, 1")]
        [TestCase("Func<int,int> temp = x => { if (true) return 1; return x; }", SearchMode.Recursive, "1, x")]
        [TestCase("Func<int,int> temp = x => { if (true) return 1; return x; }", SearchMode.TopLevel, "1, x")]
        [TestCase("Func<int,int> temp = x => { if (true) return 1; return 2; }", SearchMode.Recursive, "1, 2")]
        [TestCase("Func<int,int> temp = x => { if (true) return 1; return 2; }", SearchMode.TopLevel, "1, 2")]
        public void Lambda(string code, SearchMode search, string expected)
        {
            var testCode = @"
namespace RoslynSandbox
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
            using (var pooled = ReturnValueWalker.Create(value, search, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("await Task.Run(() => 1)", SearchMode.Recursive, "1")]
        [TestCase("await Task.Run(() => 1)", SearchMode.TopLevel, "1")]
        [TestCase("await Task.Run(() => 1).ConfigureAwait(false)", SearchMode.Recursive, "1")]
        [TestCase("await Task.Run(() => 1).ConfigureAwait(false)", SearchMode.TopLevel, "1")]
        [TestCase("await Task.Run(() => new Disposable())", SearchMode.Recursive, "new Disposable()")]
        [TestCase("await Task.Run(() => new Disposable())", SearchMode.TopLevel, "new Disposable()")]
        [TestCase("await Task.Run(() => new Disposable()).ConfigureAwait(false)", SearchMode.Recursive, "new Disposable()")]
        [TestCase("await Task.Run(() => new Disposable()).ConfigureAwait(false)", SearchMode.TopLevel, "new Disposable()")]
        [TestCase("await Task.Run(() => new string(' ', 1))", SearchMode.Recursive, "new string(' ', 1)")]
        [TestCase("await Task.Run(() => new string(' ', 1))", SearchMode.TopLevel, "new string(' ', 1)")]
        [TestCase("await Task.Run(() => new string(' ', 1)).ConfigureAwait(false)", SearchMode.Recursive, "new string(' ', 1)")]
        [TestCase("await Task.Run(() => new string(' ', 1)).ConfigureAwait(false)", SearchMode.TopLevel, "new string(' ', 1)")]
        [TestCase("await Task.Run(() => CreateInt())", SearchMode.Recursive, "1")]
        [TestCase("await Task.Run(() => CreateInt())", SearchMode.TopLevel, "CreateInt()")]
        [TestCase("await Task.Run(() => CreateInt()).ConfigureAwait(false)", SearchMode.Recursive, "1")]
        [TestCase("await Task.Run(() => CreateInt()).ConfigureAwait(false)", SearchMode.TopLevel, "CreateInt()")]
        [TestCase("await Task.FromResult(new string(' ', 1))", SearchMode.Recursive, "new string(' ', 1)")]
        [TestCase("await Task.FromResult(new string(' ', 1))", SearchMode.TopLevel, "new string(' ', 1)")]
        [TestCase("await Task.FromResult(new string(' ', 1)).ConfigureAwait(false)", SearchMode.Recursive, "new string(' ', 1)")]
        [TestCase("await Task.FromResult(new string(' ', 1)).ConfigureAwait(false)", SearchMode.TopLevel, "new string(' ', 1)")]
        [TestCase("await Task.FromResult(CreateInt())", SearchMode.Recursive, "1")]
        [TestCase("await Task.FromResult(CreateInt())", SearchMode.TopLevel, "CreateInt()")]
        [TestCase("await Task.FromResult(CreateInt()).ConfigureAwait(false)", SearchMode.Recursive, "1")]
        [TestCase("await Task.FromResult(CreateInt()).ConfigureAwait(false)", SearchMode.TopLevel, "CreateInt()")]
        [TestCase("await CreateAsync(0)", SearchMode.Recursive, "1, 0, 2, 3")]
        [TestCase("await CreateAsync(0)", SearchMode.TopLevel, "1, 0, 2, 3")]
        [TestCase("await CreateAsync(0).ConfigureAwait(false)", SearchMode.Recursive, "1, 0, 2, 3")]
        [TestCase("await CreateAsync(0).ConfigureAwait(false)", SearchMode.TopLevel, "1, 0, 2, 3")]
        [TestCase("await CreateStringAsync()", SearchMode.Recursive, "new string(' ', 1)")]
        [TestCase("await CreateStringAsync()", SearchMode.TopLevel, "new string(' ', 1)")]
        [TestCase("await ReturnAwaitTaskRunAsync()", SearchMode.Recursive, "new string(' ', 1)")]
        [TestCase("await ReturnAwaitTaskRunAsync()", SearchMode.TopLevel, "new string(' ', 1)")]
        [TestCase("await RecursiveAsync()", SearchMode.Recursive, "")]
        [TestCase("await RecursiveAsync()", SearchMode.TopLevel, "RecursiveAsync()")]
        [TestCase("await RecursiveAsync(1)", SearchMode.Recursive, "")]
        [TestCase("await RecursiveAsync(1)", SearchMode.TopLevel, "RecursiveAsync(arg)")]
        [TestCase("await RecursiveAsync1(1)", SearchMode.Recursive, "")]
        [TestCase("await RecursiveAsync1(1)", SearchMode.TopLevel, "await RecursiveAsync2(value)")]
        [TestCase("await RecursiveAsync3(1)", SearchMode.Recursive, "")]
        [TestCase("await RecursiveAsync3(1)", SearchMode.TopLevel, "RecursiveAsync4(value)")]
        public void AsyncAwait(string code, SearchMode search, string expected)
        {
            var testCode = @"
namespace RoslynSandbox
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

		private static async Task<int> RecursiveAsync1(int value)
        {
            return await RecursiveAsync2(value);
        }
		
        private static async Task<int> RecursiveAsync2(int value)
        {
            return await RecursiveAsync1(value);
        }

		private static async Task<int> RecursiveAsync3(int value)
        {
            return RecursiveAsync4(value);
        }
		
        private static async Task<int> RecursiveAsync4(int value)
        {
            return await RecursiveAsync3(value);
        }
    }
}";
            testCode = testCode.AssertReplace("// Meh()", code);
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.BestMatch<EqualsValueClauseSyntax>(code).Value;
            using (var pooled = ReturnValueWalker.Create(value, search, semanticModel, CancellationToken.None))
            {
                Assert.AreEqual(expected, string.Join(", ", pooled.Item));
            }
        }
    }
}