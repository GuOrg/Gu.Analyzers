namespace Gu.Analyzers.Test.Helpers
{
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;

    using NUnit.Framework;

    internal class AssignedValueWalkerTests
    {
        [TestCase("1")]
        [TestCase("abc")]
        [TestCase("default(int)")]
        [TestCase("typeof(int)")]
        [TestCase("nameof(int)")]
        public void LocalInitializedWithConstant(string code)
        {
            var testCode = @"
internal class Foo
{
    internal Foo()
    {
        var value = 1;
        var temp = value;
    }
}";
            testCode = testCode.AssertReplace("1", code);
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause("var temp = value;").Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual(code, actual);
            }
        }

        [Test]
        public void LocalInitializedWithDefaultGeneric()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo<T>
{
    internal Foo()
    {
        var value = default(T);
        var temp = value;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause("var temp = value;").Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual("default(T)", actual);
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
        public void LocalOutParameterGeneric()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo<T>
{
    internal Foo()
    {
        T value;
        Assign(out value);
        var temp = value;
    }

    internal void Assign(out T value)
    {
        value = default(T);
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause("var temp = value;").Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues);
                Assert.AreEqual("value, default(T)", actual);
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

        [TestCase("var temp1 = this.Value;", "1")]
        [TestCase("var temp2 = this.Value;", "1")]
        public void FieldInitializedInChainedWithLiteral(string code, string expected)
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
                Assert.AreEqual("1", actual);
            }
        }
    }
}