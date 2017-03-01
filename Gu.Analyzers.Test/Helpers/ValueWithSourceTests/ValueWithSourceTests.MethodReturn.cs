//namespace Gu.Analyzers.Test.Helpers
//{
//    using System.Linq;
//    using System.Threading;

//    using Microsoft.CodeAnalysis.CSharp;

//    using NUnit.Framework;

//    internal partial class ValueWithSourceTests
//    {
//        public class MethodReturn
//        {
//            [Test]
//            public void StaticMethodReturningNewStatementnBody()
//            {
//                var syntaxTree = CSharpSyntaxTree.ParseText(@"
//internal class Foo
//{
//    internal void Bar()
//    {
//        var text = Create();
//    }

//    internal static string Create()
//    {
//        return new string(' ', 1);
//    }
//}");
//                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
//                var semanticModel = compilation.GetSemanticModel(syntaxTree);
//                var node = syntaxTree.EqualsValueClause("var text = Create();").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual("Create() Calculated, new string(' ', 1) Created", actual);
//                }
//            }

//            [Test]
//            public void StaticMethodReturningNewExpressionBody()
//            {
//                var syntaxTree = CSharpSyntaxTree.ParseText(@"
//using System;
//internal class Foo
//{
//    internal static async Task Bar()
//    {
//        var stream = Create();
//    }

//    internal static async IDisposable Create() => new Disposable();
//}");
//                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
//                var semanticModel = compilation.GetSemanticModel(syntaxTree);
//                var node = syntaxTree.EqualsValueClause("var stream = Create();").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual("Create() Calculated, new Disposable() Created", actual);
//                }
//            }

//            [Test]
//            public void StaticMethodReturningNewInIfElse()
//            {
//                var syntaxTree = CSharpSyntaxTree.ParseText(@"
//    internal class Foo
//    {
//        internal void Bar()
//        {
//            var text = Create(true);
//        }

//        internal static string Create(bool value)
//        {
//            if (value)
//            {
//                return new string('1', 1);
//            }
//            else
//            {
//                return new string('0', 1);
//            }
//        }
//    }");
//                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
//                var semanticModel = compilation.GetSemanticModel(syntaxTree);
//                var node = syntaxTree.EqualsValueClause("var text = Create(true);").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual("Create(true) Calculated, new string('1', 1) Created, new string('0', 1) Created", actual);
//                }
//            }

//            [Test]
//            public void StaticMethodReturningFileOpenRead()
//            {
//                var syntaxTree = CSharpSyntaxTree.ParseText(@"
//using System;
//using System.IO;

//public static class Foo
//{
//    public static long Bar()
//    {
//        var value = GetStream();
//    }

//    public static Stream GetStream()
//    {
//        return File.OpenRead(""A"");
//    }
//}");
//                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
//                var semanticModel = compilation.GetSemanticModel(syntaxTree);
//                var node = syntaxTree.EqualsValueClause("var value = GetStream();").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual(@"GetStream() Calculated, File.OpenRead(""A"") External", actual);
//                }
//            }

//            [Test]
//            public void StaticMethodWithOptionalParameterReturningParameter()
//            {
//                var syntaxTree = CSharpSyntaxTree.ParseText(@"
//namespace RoslynSandBox
//{
//    using System.Collections.Generic;

//    public class Foo
//    {
//        public void Bar()
//        {
//            var temp = Bar(1);
//        }

//        private static int Bar(int value, IEnumerable<int> values = null)
//        {
//            if (values == null)
//            {
//                return Bar(value, new[] { value });
//            }

//            return value;
//        }
//    }
//}");
//                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
//                var semanticModel = compilation.GetSemanticModel(syntaxTree);
//                var node = syntaxTree.EqualsValueClause("var temp = Bar(1);").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual("Bar(1) Calculated, Bar(value, new[] { value }) Recursion, value Recursion, 1 Constant", actual);
//                }
//            }

//            [TestCase("internal static")]
//            [TestCase("public")]
//            [TestCase("private")]
//            public void MethodReturningArgOrConst(string modifiers)
//            {
//                var testCode = @"
//namespace RoslynSandBox
//{
//    internal class Foo
//    {
//        internal void Bar()
//        {
//            var value = Create(6);
//        }

//        internal static int Create(int value)
//        {
//            if (value > 5)
//            {
//                return value;
//            }

//            return 1;
//        }
//    }
//}";
//                testCode = testCode.AssertReplace("internal static", modifiers);
//                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
//                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
//                var semanticModel = compilation.GetSemanticModel(syntaxTree);
//                var node = syntaxTree.EqualsValueClause("var value = Create(6);").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual("Create(6) Calculated, value Argument, 6 Constant, 1 Constant", actual);
//                }
//            }

//            [TestCase("internal static")]
//            [TestCase("public")]
//            [TestCase("private")]
//            public void AssignedParemeter(string modifiers)
//            {
//                var testCode = @"
//internal class Foo
//{
//    internal void Bar()
//    {
//        var value = Create(1);
//    }

//    internal static int Create(int value)
//    {
//        value = 2;
//        return value;
//    }
//}";
//                testCode = testCode.AssertReplace("internal static", modifiers);
//                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
//                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
//                var semanticModel = compilation.GetSemanticModel(syntaxTree);
//                var node = syntaxTree.EqualsValueClause("var value = Create(1);").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    //// this is not perfect but keeping it like this.
//                    Assert.AreEqual("Create(1) Calculated, value Argument, 2 Constant, 1 Constant", actual);
//                }
//            }

//            [TestCase("internal static")]
//            [TestCase("public")]
//            [TestCase("private")]
//            public void IdentityMethod(string modifiers)
//            {
//                var testCode = @"
//internal class Foo
//{
//    internal void Bar()
//    {
//        var value = Id(1);
//    }

//    internal static int Id(int value)
//    {
//        return value;
//    }
//}";
//                testCode = testCode.AssertReplace("internal static", modifiers);
//                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
//                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
//                var semanticModel = compilation.GetSemanticModel(syntaxTree);
//                var node = syntaxTree.EqualsValueClause("var value = Id(1);").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual("Id(1) Calculated, value Argument, 1 Constant", actual);
//                }
//            }

//            [TestCase("var temp1 = this.Id(1);", "this.Id(1) Calculated, arg Argument, 1 Constant")]
//            [TestCase("var temp2 = this.Id(1);", "this.Id(1) Calculated, arg Argument, 1 Constant")]
//            public void IdentityMethod(string code, string expected)
//            {
//                var testCode = @"
//namespace RoslynSandBox
//{
//    using System;

//    public class Foo
//    {
//        public Foo()
//        {
//            var temp1 = this.Id(1);
//        }

//        public int Id(int arg)
//        {
//            return arg;
//        }

//        public Bar()
//        {
//            var temp2 = this.Id(1);
//        }
//    }
//}";
//                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
//                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
//                var semanticModel = compilation.GetSemanticModel(syntaxTree);
//                var node = syntaxTree.EqualsValueClause(code).Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual(expected, actual);
//                }
//            }

//            [TestCase("internal static")]
//            [TestCase("public")]
//            [TestCase("private")]
//            public void ChainedIdentityMethodStatementBody(string modifiers)
//            {
//                var testCode = @"
//internal class Foo
//{
//    internal void Bar()
//    {
//        var value = Id1(1);
//    }

//    internal static string Id1(int value1)
//    {
//        return Id2(value1);
//    }

//    internal static string Id2(int value2)
//    {
//        return value2;
//    }
//}";
//                testCode = testCode.AssertReplace("internal static", modifiers);
//                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
//                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
//                var semanticModel = compilation.GetSemanticModel(syntaxTree);
//                var node = syntaxTree.EqualsValueClause("var value = Id1(1);").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual("Id1(1) Calculated, Id2(value1) Calculated, value2 Argument, value1 Argument, 1 Constant", actual);
//                }
//            }

//            [TestCase("internal static")]
//            [TestCase("public")]
//            [TestCase("private")]
//            public void ChainedIdentityMethodExpressionBody(string modifiers)
//            {
//                var testCode = @"
//internal class Foo
//{
//    internal void Bar()
//    {
//        var value = Id1(1);
//    }

//    internal static string Id1(int value1)
//    {
//        return Id2(value1);
//    }

//    internal static string Id2(int value2)
//    {
//        return value2;
//    }
//}";
//                testCode = testCode.AssertReplace("internal static", modifiers);
//                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
//                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
//                var semanticModel = compilation.GetSemanticModel(syntaxTree);
//                var node = syntaxTree.EqualsValueClause("var value = Id1(1);").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual("Id1(1) Calculated, Id2(value1) Calculated, value2 Argument, value1 Argument, 1 Constant", actual);
//                }
//            }

//            [Test]
//            public void VariableAssignedWithOutParameter()
//            {
//                var syntaxTree = CSharpSyntaxTree.ParseText(@"
//internal class Foo
//{
//    internal void Bar()
//    {
//        int value;
//        this.Assign(out value);
//        var temp = value;
//    }

//    private void Assign(out int value)
//    {
//        value = 1;
//    }
//}");
//                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
//                var semanticModel = compilation.GetSemanticModel(syntaxTree);
//                var node = syntaxTree.EqualsValueClause("var temp = value;").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual("this.Assign(out value) Out, 1 Constant", actual);
//                }
//            }

//            [Test]
//            public void VariableAssignedWithOutParameterAssignedTwice()
//            {
//                var syntaxTree = CSharpSyntaxTree.ParseText(@"
//internal class Foo
//{
//    internal void Bar()
//    {
//        int value;
//        this.Assign(out value);
//        var temp = value;
//    }

//    private void Assign(out int value)
//    {
//        value = 1;
//        value = 2;
//    }
//}");
//                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
//                var semanticModel = compilation.GetSemanticModel(syntaxTree);
//                var node = syntaxTree.EqualsValueClause("var temp = value;").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual("this.Assign(out value) Out, 1 Constant, 2 Constant", actual);
//                }
//            }

//            [Test]
//            public void VariableAssignedWithChainedOutParameter()
//            {
//                var syntaxTree = CSharpSyntaxTree.ParseText(@"
//internal class Foo
//{
//    internal void Bar()
//    {
//        int value;
//        this.Assign1(out value);
//        var temp = value;
//        var meh = temp;
//    }

//    private void Assign1(out int value1)
//    {
//        this.Assign2(out value1);
//    }

//    private void Assign2(out int value2)
//    {
//        value2 = 1;
//    }
//}");
//                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
//                var semanticModel = compilation.GetSemanticModel(syntaxTree);
//                var node = syntaxTree.EqualsValueClause("var meh = temp;").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual("this.Assign1(out value) Out, this.Assign2(out value1) Out, 1 Constant", actual);
//                }
//            }

//            [Test]
//            public void VariableAssignedWithRefParameter()
//            {
//                var syntaxTree = CSharpSyntaxTree.ParseText(@"
//internal class Foo
//{
//    private int field;

//    internal void Bar()
//    {
//        int value;
//        this.Assign(ref value);
//        var temp = value;
//        var meh = temp;
//    }

//    private void Assign(ref int value)
//    {
//        value = 1;
//    }
//}");
//                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
//                var semanticModel = compilation.GetSemanticModel(syntaxTree);
//                var node = syntaxTree.EqualsValueClause("var temp = value;").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual("this.Assign(ref value) Ref, 1 Constant", actual);
//                }

//                node = syntaxTree.EqualsValueClause("var meh = temp;").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual("this.Assign(ref value) Ref, 1 Constant", actual);
//                }
//            }
//        }
//    }
//}