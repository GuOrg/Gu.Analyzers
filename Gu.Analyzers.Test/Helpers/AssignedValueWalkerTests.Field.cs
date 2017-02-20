namespace Gu.Analyzers.Test.Helpers
{
    using System.Threading;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    internal partial class AssignedValueWalkerTests
    {
        internal class Field
        {
            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.temp1;", "1")]
            [TestCase("var temp3 = this.value;", "1")]
            [TestCase("var temp4 = this.temp1;", "1")]
            public void InitializedlWithLiteral(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    private readonly int value = 1;
    private readonly int temp1 = this.value;

    internal Foo()
    {
        var temp1 = this.value;
        var temp2 = this.temp1;
    }

    internal void Bar()
    {
        var temp3 = this.value;
        var temp4 = this.temp1;
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
            public void InitializedlWithOutParameter(string code, string expected)
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
            public void InitializedInCtorWithLiteral(string code, string expected)
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
            [TestCase("var temp2 = this.Value;", "1, 2, 3, 4")]
            public void InitializedInChainedWithLiteral(string code, string expected)
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
        this.Value = 4;
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
            public void InitializedInChainedWithPassed(string code, string expected)
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

            [TestCase("var temp1 = this.Value;", "1, 2, 3")]
            [TestCase("var temp2 = this.Value;", "1, 2, 3, 4")]
            public void InitializedInChainedWithLiteralGeneric(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo<T>
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
        this.Value = 4;
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

            [TestCase("var temp1 = this.value;", "1, 2, 3")]
            [TestCase("var temp2 = this.value;", "1, 2, 3, 4")]
            public void InitializedInImplicitBase(string code, object expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase
{
    protected readonly int value = 1;

    internal FooBase()
    {
        this.value = 2;
    }
}

internal class Foo : FooBase
{
    internal Foo()
    {
        this.value = 3;
        var temp1 = this.value;
        this.value = 4;
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

            [TestCase("var temp1 = this.value;", "1, 2, 3")]
            [TestCase("var temp2 = this.value;", "1, 2, 3, 4")]
            public void InitializedInBaseCtorWithLiteral(string code, string expected)
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
        this.value = 3;
        var temp1 = this.value;
        this.value = 4;
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
            public void InitializedInBaseCtorWithDefaultGeneric(string code, string expected)
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
            public void InitializedInBaseCtorWithDefaultGenericGeneric(string code, string expected)
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
        }
    }
}