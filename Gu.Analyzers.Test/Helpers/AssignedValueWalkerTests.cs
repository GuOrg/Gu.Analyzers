namespace Gu.Analyzers.Test.Helpers
{
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;

    using NUnit.Framework;

    internal partial class AssignedValueWalkerTests
    {
        [TestCase("var temp2 = this.value;", "1")]
        public void FieldInitializedlWithLiteral(string code, string expected)
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
            var value = syntaxTree.EqualsValueClause(code).Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.value;", "this.value, 1")]
        [TestCase("var temp2 = this.value;", "this.value, 1")]
        public void FieldInitializedlWithOutParameter(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandBox
{
    internal class Foo
    {
        private int value;

        public Foo()
        {
            this.Assign(out this.value);
            var temp1 = this.value;
        }

        internal void Bar()
        {
            var temp2 = this.value;
        }

        private void Assign(out int outValue)
        {
            outValue = 1;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause(code).Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.value;", "1, 2")]
        [TestCase("var temp2 = this.value;", "1, 2")]
        public void FieldInitializedInCtorWithLiteral(string code, string expected)
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
            var value = syntaxTree.EqualsValueClause(code).Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.Value;", "1, 2, 3")]
        [TestCase("var temp2 = this.Value;", "1, 2, 3")]
        public void FieldInitializedInChainedWithLiteral(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        this.Value = 2;
    }

    internal Foo(string text)
        : this()
    {
        this.Value = 3;
        var temp1 = this.Value;
    }

    public int Value { get; set; } = 1;

    internal void Bar()
    {
        var temp2 = this.Value;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause(code).Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.Value;", "value")]
        [TestCase("var temp2 = this.Value;", "value")]
        public void FieldInitializedInChainedWithPassed(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo(int value)
    {
        this.Value = value;
    }

    internal Foo()
        : this(1)
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
            var value = syntaxTree.EqualsValueClause(code).Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.Value;", "1")]
        [TestCase("var temp2 = this.Value;", "1")]
        public void FieldInitializedInChainedWithLiteralGeneric(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo<T>
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
            var value = syntaxTree.EqualsValueClause(code).Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.value;", "1")]
        [TestCase("var temp2 = this.value;", "1")]
        public void FieldInitializedInBaseWithLiteral(string code, object expected)
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
            var value = syntaxTree.EqualsValueClause(code).Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.value;", "1, 2")]
        [TestCase("var temp2 = this.value;", "1, 2")]
        public void FieldInitializedInBaseCtorWithLiteral(string code, string expected)
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
            var value = syntaxTree.EqualsValueClause(code).Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.value;", "default(T)")]
        [TestCase("var temp2 = this.value;", "default(T)")]
        public void FieldInitializedInBaseCtorWithDefaultGeneric(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase<T>
{
    protected readonly T value;
    
    internal FooBase()
    {
        this.value = default(T);
    }

    internal FooBase(T value)
    {
        this.value = value;
    }
}

internal class Foo : FooBase<int>
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
            var value = syntaxTree.EqualsValueClause(code).Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.value;", "default(T)")]
        [TestCase("var temp2 = this.value;", "default(T)")]
        public void FieldInitializedInBaseCtorWithDefaultGenericGeneric(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase<T>
{
    protected readonly T value;
    
    internal FooBase()
    {
        this.value = default(T);
    }

    internal FooBase(T value)
    {
        this.value = value;
    }
}

internal class Foo<T> : FooBase<T>
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
            var value = syntaxTree.EqualsValueClause(code).Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual(expected, actual);
            }
        }

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
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
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
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
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
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
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