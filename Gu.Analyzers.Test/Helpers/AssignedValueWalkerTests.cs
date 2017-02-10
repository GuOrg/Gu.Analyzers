namespace Gu.Analyzers.Test.Helpers
{
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;

    using NUnit.Framework;

    internal class AssignedValueWalkerTests
    {
        [Test]
        public void LocalInitializedWithLiteral()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        var value = 1;
        var temp = value;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause("var temp = value;").Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual("1", actual);
            }
        }

        [Test]
        public void LocalOutParameter()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        int value;
        Assign(out value);
        var temp = value;
    }

    internal void Assign(out int value)
    {
        value = 1;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause("var temp = value;").Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual("value, 1", actual);
            }
        }

        [Test]
        public void LocalChainedOutParameter()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        int value;
        Assign1(out value);
        var temp = value;
    }

    internal void Assign1(out int value1)
    {
         Assign2(out value1);
    }

    internal void Assign2(out int value2)
    {
        value2 = 1;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause("var temp = value;").Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual("value, value1, 1", actual);
            }
        }

        [Test]
        public void LocalRefParameter()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        int value;
        Assign(ref value);
        var temp = value;
    }

    internal void Assign(ref int value)
    {
        value = 1;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause("var temp = value;").Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual("value, 1", actual);
            }
        }

        [Test]
        public void LocalNotInitialized()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        int value;
        var temp = value;
        value = 1;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause("var temp = value;").Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual(string.Empty, actual);
            }
        }

        [Test]
        public void FieldInitializedlWithLiteral()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    private readonly int value = 1;
    private readonly int temp1 = this.value;

    internal Foo()
    {
        var temp2 = this.value;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause("var temp2 = this.value;").Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual("1", actual);
            }

            value = syntaxTree.EqualsValueClause("temp1 = this.value;").Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual("1", actual);
            }
        }

        [Test]
        public void FieldInitializedInCtorWithLiteral()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    private readonly int value = 1;

    internal Foo()
    {
        this.value = 2;
        var temp1 = this.value;
    }

    internal void Bar()
    {
        var temp2 = this.value;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause("var temp1 = this.value;").Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual("1, 2", actual);
            }

            value = syntaxTree.EqualsValueClause("var temp2 = this.value;").Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual("1, 2", actual);
            }
        }

        [Test]
        public void FieldInitializedInChainedWithLiteral()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        this.Value = 1;
    }

    internal Foo(string text)
        : this()
    {
        var temp1 = this.Value;
    }

    public int Value { get; set; }

    internal void Bar()
    {
        var temp2 = this.Value;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause("var temp1 = this.Value;").Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual("1", actual);
            }

            value = syntaxTree.EqualsValueClause("var temp2 = this.Value;").Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual("1", actual);
            }
        }

        [Test]
        public void FieldInitializedInBaseWithLiteral()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase
{
    protected readonly int value = 1;
}

internal class Foo : FooBase
{
    internal Foo()
    {
        var temp1 = this.value;
    }

    internal void Bar()
    {
        var temp2 = this.value;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause("var temp1 = this.value;").Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual("1", actual);
            }

            value = syntaxTree.EqualsValueClause("var temp2 = this.value;").Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual("1", actual);
            }
        }

        [Test]
        public void FieldInitializedInBaseCtorWithLiteral()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase
{
    protected readonly int value = 1;
    
    internal FooBase()
    {
        this.value = 2;
    }

    internal FooBase(int value)
    {
        this.value = value;
    }
}

internal class Foo : FooBase
{
    internal Foo()
    {
        var temp1 = this.value;
    }

    internal void Bar()
    {
        var temp2 = this.value;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause("var temp1 = this.value;").Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual("1, 2", actual);
            }

            value = syntaxTree.EqualsValueClause("var temp2 = this.value;").Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual("1, 2", actual);
            }
        }
    }
}