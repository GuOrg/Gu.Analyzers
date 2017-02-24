namespace Gu.Analyzers.Test.Helpers
{
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;

    using NUnit.Framework;

    internal partial class AssignedValueWalkerTests
    {
        [Test]
        public void ArrayIndexer()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        var ints = new int[2];
        ints[0] = 1;
        var temp = ints[0];
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause("var temp = ints[0];").Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual("1", actual);
            }
        }

        [Test]
        public void ListOfIntIndexer()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandBox
{
    using System.Collections.Generic;

    internal class Foo
    {
        internal Foo()
        {
            var ints = new List<int> { 1, 2 };
            ints[0] = 3;
            var temp = ints[0];
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause("var temp = ints[0];").Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual("3", actual);
            }
        }
    }
}