//namespace Gu.Analyzers.Test.Helpers
//{
//    using System.Linq;
//    using System.Threading;

//    using Microsoft.CodeAnalysis.CSharp;

//    using NUnit.Framework;

//    internal partial class ValueWithSourceTests
//    {
//        public class Field
//        {
//            [TestCase("var temp1 = this.value;", "this.value Member, Assign(out this.value) Out, 1 Constant")]
//            [TestCase("var temp2 = this.value;", "this.value Member, Assign(out this.value) Out, 1 Constant")]
//            public void PrivateAssignedWithOutParameterBeforeInCtor(string code, string expected)
//            {
//                var testCode = @"
//namespace RoslynSandBox
//{
//    internal class Foo
//    {
//        private int value;

//        public Foo()
//        {
//            Assign(out this.value);
//            var temp1 = this.value;
//        }

//        internal void Bar()
//        {
//            var temp2 = this.value;
//        }

//        private static void Assign(out int outValue)
//        {
//            outValue = 1;
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

//            [TestCase("private void Assign")]
//            [TestCase("public void Assign")]
//            [TestCase("public static void Assign")]
//            public void PrivateAssignedWithOutParameterBeforeInCtorWithModifiers(string code)
//            {
//                var testCode = @"
//internal class Foo
//{
//    private int field;

//    public Foo()
//    {
//        this.Assign(out this.field);
//        var temp1 = this.field;
//    }

//    internal void Bar()
//    {
//        var temp2 = this.field;
//    }

//    private void Assign(out int value)
//    {
//        value = 1;
//    }
//}";
//                testCode = testCode.AssertReplace("private void Assign", code);
//                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
//                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
//                var semanticModel = compilation.GetSemanticModel(syntaxTree);
//                var node = syntaxTree.EqualsValueClause("var temp1 = this.field;").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual("this.field Member, this.Assign(out this.field) Out, 1 Constant", actual);
//                }

//                node = syntaxTree.EqualsValueClause("var temp2 = this.field;").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual("this.field Member, this.Assign(out this.field) Out, 1 Constant", actual);
//                }
//            }

//            [TestCase("var temp1 = this.field;", "this.field Member", "private void Assign")]
//            [TestCase("var temp1 = this.field;", "this.field Member", "public void Assign")]
//            [TestCase("var temp1 = this.field;", "this.field Member", "public static void Assign")]
//            [TestCase("var temp2 = this.field;", "this.field Member, this.Assign(out this.field) Out, 1 Constant", "private void Assign")]
//            [TestCase("var temp2 = this.field;", "this.field Member, this.Assign(out this.field) Out, 1 Constant", "public void Assign")]
//            [TestCase("var temp2 = this.field;", "this.field Member, this.Assign(out this.field) Out, 1 Constant", "public static void Assign")]
//            public void PrivateAssignedWithOutParameterAfterInCtor(string code, string expected, string modifiers)
//            {
//                var testCode = @"
//internal class Foo
//{
//    private int field;

//    internal Foo()
//    {
//        var temp1 = this.field;
//        this.Assign(out this.field);
//    }

//    internal void Bar()
//    {
//        var temp2 = this.field;
//    }

//    private void Assign(out int value)
//    {
//        value = 1;
//    }
//}";
//                testCode = testCode.AssertReplace("private void Assign", modifiers);
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

//            [TestCase("private void Assign")]
//            [TestCase("public void Assign")]
//            [TestCase("public static void Assign")]
//            public void PrivateAssignedWithRefParameterAfter(string code)
//            {
//                var testCode = @"
//internal class Foo
//{
//    private int field;

//    internal Foo()
//    {
//        var temp1 = this.field;
//        Assign(ref this.field);
//    }

//    internal void Bar()
//    {
//        var temp2 = this.field;
//    }

//    private void Assign(ref int value)
//    {
//        value = 1;
//    }
//}";
//                testCode = testCode.AssertReplace("private void Assign", code);
//                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
//                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
//                var semanticModel = compilation.GetSemanticModel(syntaxTree);
//                var node = syntaxTree.EqualsValueClause("var temp1 = this.field;").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual("this.field Member", actual);
//                }

//                node = syntaxTree.EqualsValueClause("var temp2 = this.field;").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual("this.field Member, Assign(ref this.field) Ref, 1 Constant", actual);
//                }
//            }

//            [TestCase("var temp1 = this.field;", "this.field Member, 1 Constant")]
//            [TestCase("var temp2 = this.field;", "this.field Member, this.field PotentiallyInjected, 1 Constant")]
//            public void PublicInitialized(string code, string expected)
//            {
//                var syntaxTree = CSharpSyntaxTree.ParseText(@"
//internal class Foo
//{
//    public int field = 1;

//    internal Foo()
//    {
//        var temp1 = this.field;
//    }

//    internal void Bar()
//    {
//        var temp2 = this.field;
//    }
//}");
//                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
//                var semanticModel = compilation.GetSemanticModel(syntaxTree);
//                var node = syntaxTree.EqualsValueClause(code).Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual(expected, actual);
//                }
//            }

//            [TestCase("public readonly")]
//            [TestCase("private readonly")]
//            [TestCase("private")]
//            public void Initialized(string modifiers)
//            {
//                var testCode = @"
//internal class Foo
//{
//    public readonly int field = 1;

//    internal Foo()
//    {
//        var temp1 = this.field;
//    }

//    internal void Bar()
//    {
//        var temp2 = this.field;
//    }
//}";
//                testCode = testCode.AssertReplace("public readonly", modifiers);
//                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
//                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
//                var semanticModel = compilation.GetSemanticModel(syntaxTree);
//                var node = syntaxTree.EqualsValueClause("var temp1 = this.field;").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual("this.field Member, 1 Constant", actual);
//                }

//                node = syntaxTree.EqualsValueClause("var temp2 = this.field;").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual("this.field Member, 1 Constant", actual);
//                }
//            }
//        }
//    }
//}