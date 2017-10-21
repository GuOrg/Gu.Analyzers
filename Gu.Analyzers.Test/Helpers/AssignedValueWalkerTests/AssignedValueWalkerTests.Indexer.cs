namespace Gu.Analyzers.Test.Helpers.AssignedValueWalkerTests
{
    using System.Threading;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    internal partial class AssignedValueWalkerTests
    {
        internal class Indexer
        {
            [TestCase("var temp1 = ints[0];", "1, 2")]
            [TestCase("var temp2 = ints[0];", "1, 2, 3")]
            public void InitializedArrayIndexer(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        var ints = new int[] { 1, 2 };
        var temp1 = ints[0];
        ints[0] = 3;
        var temp2 = ints[0];
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code)
                                      .Value;
                using (var pooled = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled);
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = ints[0];", "1, 2")]
            [TestCase("var temp2 = ints[0];", "1, 2, 3")]
            public void InitializedTypedArrayIndexer(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        int[] ints = { 1, 2 };
        var temp1 = ints[0];
        ints[0] = 3;
        var temp2 = ints[0];
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code)
                                      .Value;
                using (var pooled = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled);
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = ints[0];", "1, 2")]
            [TestCase("var temp2 = ints[0];", "1, 2, 3")]
            public void InitializedListOfIntIndexerAfterSetItem(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    internal class Foo
    {
        internal Foo()
        {
            var ints = new List<int> { 1, 2 };
            var temp1 = ints[0];
            ints[0] = 3;
            var temp2 = ints[0];
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code)
                                      .Value;
                using (var pooled = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled);
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = ints[0];", "1, 2")]
            [TestCase("var temp2 = ints[0];", "1, 2, 3")]
            public void InitializedListOfIntIndexerAfterAddItem(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    internal class Foo
    {
        internal Foo()
        {
            var ints = new List<int> { 1, 2 };
            var temp1 = ints[0];
            ints.Add(3);
            var temp2 = ints[0];
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code)
                                      .Value;
                using (var pooled = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled);
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = ints[0];", "1, 2")]
            [TestCase("var temp2 = ints[0];", "1, 2, 3")]
            public void InitializedElementStyleDictionaryIndexer(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    internal class Foo
    {
        internal Foo()
        {
            var ints = new Dictionary<int, int> 
            { 
                [1] = 1,
                [2] = 2,
            };
            var temp1 = ints[0];
            ints[3] = 3;
            var temp2 = ints[0];
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code)
                                      .Value;
                using (var pooled = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled);
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = ints[0];", "1, 2")]
            [TestCase("var temp2 = ints[0];", "1, 2, 3")]
            public void InitializedDictionaryIndexer(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    internal class Foo
    {
        internal Foo()
        {
            var ints = new Dictionary<int, int> 
            {
                { 1, 1 }, 
                { 2, 2 }, 
            };
            var temp1 = ints[0];
            ints[3] = 3;
            var temp2 = ints[0];
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code)
                                      .Value;
                using (var pooled = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled);
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = ints[0];", "1, 2")]
            [TestCase("var temp2 = ints[0];", "1, 2, 3")]
            public void InitializedDictionaryAfterAdd(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    internal class Foo
    {
        internal Foo()
        {
            var ints = new Dictionary<int, int> 
            {
                { 1, 1 }, 
                { 2, 2 }, 
            };
            var temp1 = ints[0];
            ints.Add(3, 3);
            var temp2 = ints[0];
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code)
                                      .Value;
                using (var pooled = AssignedValueWalker.Borrow(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled);
                    Assert.AreEqual(expected, actual);
                }
            }
        }
    }
}