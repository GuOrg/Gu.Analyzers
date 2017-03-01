//namespace Gu.Analyzers.Test.Helpers
//{
//    using System.Linq;
//    using System.Threading;

//    using Microsoft.CodeAnalysis.CSharp;

//    using NUnit.Framework;

//    internal partial class ValueWithSourceTests
//    {
//        internal class Indexer
//        {
//            [TestCase("{ 1, 2, 3 }")]
//            [TestCase("new [] { 1, 2, 3 }")]
//            [TestCase("new int[] { 1, 2, 3 }")]
//            public void ArrayCreationField(string collection)
//            {
//                var testCode = @"
//internal class Foo
//{
//    public readonly int[] ints = { 1, 2, 3 };

//    internal Foo()
//    {
//        var temp1 = this.ints[1];
//    }
//}";
//                testCode = testCode.AssertReplace("{ 1, 2, 3 }", collection);
//                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
//                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
//                var semanticModel = compilation.GetSemanticModel(syntaxTree);
//                var node = syntaxTree.EqualsValueClause("var temp1 = this.ints[1];").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual($"this.ints[1] Member, this.ints Member, {collection} Created", actual);
//                }
//            }

//            [TestCase("{ 1, 2, 3 }")]
//            [TestCase("new [] { 1, 2, 3 }")]
//            [TestCase("new int[] { 1, 2, 3 }")]
//            public void ArrayCreationProperty(string collection)
//            {
//                var testCode = @"
//internal class Foo
//{
//    internal Foo()
//    {
//        var temp1 = this.Ints[1];
//    }

//    public int[] Ints { get; } = { 1, 2, 3 };
//}";
//                testCode = testCode.AssertReplace("{ 1, 2, 3 }", collection);
//                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
//                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
//                var semanticModel = compilation.GetSemanticModel(syntaxTree);
//                var node = syntaxTree.EqualsValueClause("var temp1 = this.Ints[1];").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual($"this.Ints[1] Member, this.Ints Member, {collection} Created", actual);
//                }
//            }

//            [TestCase("var temp1 = this.ints[1];", "this.ints[1] Member, this.ints Member, { 1, 2, 3 } Created")]
//            [TestCase("var temp2 = this.Ints[1];", "this.Ints[1] Member, this.Ints Member, { 1, 2, 3 } Created")]
//            [TestCase("var temp3 = this.ints[1];", "this.ints[1] Member, this.ints[1] PotentiallyInjected, this.ints Member, this.ints PotentiallyInjected, { 1, 2, 3 } Created")]
//            [TestCase("var temp4 = this.Ints[1];", "this.Ints[1] Member, this.Ints[1] PotentiallyInjected, this.Ints Member, this.Ints PotentiallyInjected, { 1, 2, 3 } Created")]
//            public void PublicMutableMemberArrayInitializedArrayThenAccessedWithIndexer(string code, string expected)
//            {
//                var testCode = @"
//internal class Foo
//{
//    public int[] ints = { 1, 2, 3 };

//    internal Foo()
//    {
//        var temp1 = this.ints[1];
//        var temp2 = this.Ints[1];
//    }

//    public int[] Ints { get; set; } = { 1, 2, 3 };

//    internal void Bar()
//    {
//        var temp3 = this.ints[1];
//        var temp4 = this.Ints[1];
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

//            [TestCase("var temp1 = this.ints[1];", "this.ints[1] Member, this.ints Member, { 1, 2, 3 } Created")]
//            [TestCase("var temp2 = this.Ints[1];", "this.Ints[1] Member, this.Ints Member, { 1, 2, 3 } Created")]
//            [TestCase("var temp3 = this.ints[1];", "this.ints[1] Member, this.ints[1] PotentiallyInjected, this.ints Member, { 1, 2, 3 } Created")]
//            [TestCase("var temp4 = this.Ints[1];", "this.Ints[1] Member, this.Ints[1] PotentiallyInjected, this.Ints Member, { 1, 2, 3 } Created")]
//            public void PublicReadonlyMemberArrayInitializedArrayThenAccessedWithIndexer(string code, string expected)
//            {
//                var testCode = @"
//internal class Foo
//{
//    public readonly int[] ints = { 1, 2, 3 };

//    internal Foo()
//    {
//        var temp1 = this.ints[1];
//        var temp2 = this.Ints[1];
//    }

//    public int[] Ints { get; } = { 1, 2, 3 };

//    internal void Bar()
//    {
//        var temp3 = this.ints[1];
//        var temp4 = this.Ints[1];
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

//            [TestCase("var temp1 = this.ints[1];", "this.ints[1] External, this.ints Member, new List<int> { 1, 2, 3 } Created")]
//            [TestCase("var temp2 = this.Ints[1];", "this.Ints[1] External, this.Ints Member, new List<int> { 1, 2, 3 } Created")]
//            [TestCase("var temp3 = this.ints[1];", "this.ints[1] External, this.ints[1] PotentiallyInjected, this.ints Member, new List<int> { 1, 2, 3 } Created")]
//            [TestCase("var temp4 = this.Ints[1];", "this.Ints[1] External, this.Ints[1] PotentiallyInjected, this.Ints Member, new List<int> { 1, 2, 3 } Created")]
//            public void PublicReadonlyListOfIntInitializedThenAccessedWithIndexer(string code, string expected)
//            {
//                var testCode = @"
//namespace RoslynSandBox
//{
//    using System.Collections.Generic;

//    internal class Foo
//    {
//        public readonly List<int> ints = new List<int> { 1, 2, 3 };

//        internal Foo()
//        {
//            var temp1 = this.ints[1];
//            var temp2 = this.Ints[1];
//        }

//        public List<int> Ints { get; } = new List<int> { 1, 2, 3 };

//        internal void Bar()
//        {
//            var temp3 = this.ints[1];
//            var temp4 = this.Ints[1];
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

//            [TestCase("var temp1 = this.ints[1];", "this.ints[1] External, this.ints Member, new List<int> { 1, 2, 3 } Created")]
//            [TestCase("var temp2 = this.Ints[1];", "this.Ints[1] External, this.Ints Member, new List<int> { 1, 2, 3 } Created")]
//            [TestCase("var temp3 = this.ints[1];", "this.ints[1] External, this.ints Member, new List<int> { 1, 2, 3 } Created")]
//            [TestCase("var temp4 = this.Ints[1];", "this.Ints[1] External, this.Ints Member, new List<int> { 1, 2, 3 } Created")]
//            public void PublicReadonlyIReadOnlyListOfIntInitializedThenAccessedWithIndexer(string code, string expected)
//            {
//                var testCode = @"
//namespace RoslynSandBox
//{
//    using System.Collections.Generic;

//    internal class Foo
//    {
//        public readonly IReadOnlyList<int> ints = new List<int> { 1, 2, 3 };

//        internal Foo()
//        {
//            var temp1 = this.ints[1];
//            var temp2 = this.Ints[1];
//        }

//        public IReadOnlyList<int> Ints { get; } = new List<int> { 1, 2, 3 };

//        internal void Bar()
//        {
//            var temp3 = this.ints[1];
//            var temp4 = this.Ints[1];
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

//            [TestCase("var temp1 = this.ints[1];", "this.ints[1] External, this.ints Member, new List<int> { 1, 2, 3 } Created")]
//            [TestCase("var temp2 = this.Ints[1];", "this.Ints[1] External, this.Ints Member, new List<int> { 1, 2, 3 } Created")]
//            [TestCase("var temp3 = this.ints[1];", "this.ints[1] External, this.ints Member, new List<int> { 1, 2, 3 } Created")]
//            [TestCase("var temp4 = this.Ints[1];", "this.Ints[1] External, this.Ints Member, new List<int> { 1, 2, 3 } Created")]
//            public void PrivateListOfIntInitializedThenAccessedWithIndexer(string code, string expected)
//            {
//                var testCode = @"
//namespace RoslynSandBox
//{
//    using System.Collections.Generic;

//    internal class Foo
//    {
//        private readonly List<int> ints = new List<int> { 1, 2, 3 };

//        internal Foo()
//        {
//            var temp1 = this.ints[1];
//            var temp2 = this.Ints[1];
//        }

//        private List<int> Ints { get; } = new List<int> { 1, 2, 3 };

//        internal void Bar()
//        {
//            var temp3 = this.ints[1];
//            var temp4 = this.Ints[1];
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
//        }
//    }
//}