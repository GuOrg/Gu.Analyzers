namespace Gu.Analyzers.Test.Helpers
{
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;

    using NUnit.Framework;

    internal partial class AssignedValueWalkerTests
    {
        [TestCase("var temp1 = this.bar;", "1, 2")]
        [TestCase("var temp2 = this.Bar;", "1, 2")]
        [TestCase("var temp3 = this.bar;", "1, 2")]
        [TestCase("var temp4 = this.Bar;", "1, 2")]
        public void BackingFieldPrivateSetInitializedAndAssignedInCtor(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
public sealed class Foo
{
    private int bar = 1;

    public Foo()
    {
        this.bar = 2;
        var temp1 = this.bar;
        var temp2 = this.Bar;
    }

    public int Bar
    {
        get { return this.bar; }
        private set { this.bar = value; }
    }

    public void Meh()
    {
        var temp3 = this.bar;
        var temp4 = this.Bar;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause(code).Value;
            using (var pooled = AssignedValueWalker_.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.bar;", "1, 2")]
        [TestCase("var temp2 = this.Bar;", "1, 2")]
        [TestCase("var temp3 = this.bar;", "1, 2, value")]
        [TestCase("var temp4 = this.Bar;", "1, 2, value")]
        public void BackingFieldPublicSetInitializedAndAssignedInCtor(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
public sealed class Foo
{
    private int bar = 1;

    public Foo()
    {
        this.bar = 2;
        var temp1 = this.bar;
        var temp2 = this.Bar;
    }

    public int Bar
    {
        get { return this.bar; }
        set { this.bar = value; }
    }

    public void Meh()
    {
        var temp3 = this.bar;
        var temp4 = this.Bar;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause(code).Value;
            using (var pooled = AssignedValueWalker_.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.bar;", "1, value, 2")]
        [TestCase("var temp2 = this.Bar;", "1, value, 2")]
        [TestCase("var temp3 = this.bar;", "1, value, 2")]
        [TestCase("var temp4 = this.Bar;", "1, value, 2")]
        public void BackingFieldPublicSetInitializedAndPropertyAssignedInCtor(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
public sealed class Foo
{
    private int bar = 1;

    public Foo()
    {
        this.Bar = 2;
        var temp1 = this.bar;
        var temp2 = this.Bar;
    }

    public int Bar
    {
        get { return this.bar; }
        private set { this.bar = value; }
    }

    public void Meh()
    {
        var temp3 = this.bar;
        var temp4 = this.Bar;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause(code).Value;
            using (var pooled = AssignedValueWalker_.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual(expected, actual);
            }
        }

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
            using (var pooled = AssignedValueWalker_.Create(value, semanticModel, CancellationToken.None))
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
            using (var pooled = AssignedValueWalker_.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual("3", actual);
            }
        }
    }
}