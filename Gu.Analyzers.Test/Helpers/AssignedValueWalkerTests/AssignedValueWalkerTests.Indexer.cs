namespace Gu.Analyzers.Test.Helpers.AssignedValueWalkerTests
{
    using System.Threading;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    internal partial class AssignedValueWalkerTests
    {
        internal class Indexer
        {
            [TestCase("var temp1 = ints[0];", "new int[] { 1, 2 }")]
            [TestCase("var temp2 = ints[0];", "new int[] { 1, 2 }, 3")]
            public void ArrayIndexer(string code, string expected)
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
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item);
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = ints[0];", "new List<int> { 1, 2 }")]
            [TestCase("var temp2 = ints[0];", "new List<int> { 1, 2 }, 3")]
            public void ListOfIntIndexerAfterSetItem(string code, string expected)
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
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item);
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = ints[0];", "new List<int> { 1, 2 }")]
            [TestCase("var temp2 = ints[0];", "new List<int> { 1, 2 }, 3")]
            public void ListOfIntIndexerAfterAddItem(string code, string expected)
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
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item);
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = ints[0];", "new Dictionary<int, int> { [0] = 1 }")]
            [TestCase("var temp2 = ints[0];", "new Dictionary<int, int> { [0] = 1 }, 2")]
            public void DictionaryIndexer(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    internal class Foo
    {
        internal Foo()
        {
            var ints = new Dictionary<int, int> { [0] = 1 };
            var temp1 = ints[0];
            ints[0] = 2;
            var temp2 = ints[0];
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code)
                                      .Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item);
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = ints[0];", "new Dictionary<int, int> { { 1, 1 }, }")]
            [TestCase("var temp2 = ints[0];", "new Dictionary<int, int> { { 1, 1 }, }, 2")]
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
            var ints = new Dictionary<int, int> { { 1, 1 }, };
            var temp1 = ints[0];
            ints[0] = 2;
            var temp2 = ints[0];
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code)
                                      .Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item);
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = ints[0];", "new Dictionary<int, int> { { 1, 1 }, }")]
            [TestCase("var temp2 = ints[0];", "new Dictionary<int, int> { { 1, 1 }, }, 2")]
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
            var ints = new Dictionary<int, int> { { 1, 1 }, };
            var temp1 = ints[0];
            ints.Add(1, 2);
            var temp2 = ints[0];
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code)
                                      .Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item);
                    Assert.AreEqual(expected, actual);
                }
            }
        }
    }
}