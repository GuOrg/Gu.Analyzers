namespace Gu.Analyzers.Test.Helpers
{
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;

    using NUnit.Framework;

    internal partial class AssignedValueWalkerTests
    {
        internal class Local
        {
            [TestCase("1")]
            [TestCase("abc")]
            [TestCase("default(int)")]
            [TestCase("typeof(int)")]
            [TestCase("nameof(int)")]
            public void InitializedWithConstant(string code)
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
            public void InitializedWithDefaultGeneric()
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

            [TestCase("var temp1 = value;", "")]
            [TestCase("var temp2 = value;", "Assign(out value)")]
            [TestCase("var temp3 = value;", "")]
            [TestCase("var temp4 = value;", "Assign(out value)")]
            public void OutParameter(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        int value;
        var temp1 = value;
        Assign(out value);
        var temp2 = value;
    }

    internal void Bar()
    {
        int value;
        var temp3 = value;
        Assign(out value);
        var temp4 = value;
    }

    internal void Assign(out int outValue)
    {
        outValue = 1;
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
            public void OutParameterGeneric()
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

    internal void Assign(out T outValue)
    {
        outValue = default(T);
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause("var temp = value;").Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item.AssignedValues);
                    Assert.AreEqual("Assign(out value)", actual);
                }
            }

            [TestCase("var temp1 = value;", "")]
            [TestCase("var temp2 = value;", "Assign1(out value)")]
            public void ChainedOutParameter(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        int value;
        var temp1 = value;
        Assign1(out value);
        var temp2 = value;
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
                var value = syntaxTree.EqualsValueClause(code).Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item.AssignedValues);
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = value;", "")]
            [TestCase("var temp2 = value;", "Assign1(ref value)")]
            public void ChainedRefParameter(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        int value;
        var temp1 = value;
        Assign1(ref value);
        var temp2 = value;
    }

    internal void Assign1(ref int value1)
    {
         Assign2(ref value1);
    }

    internal void Assign2(ref int value2)
    {
        value2 = 1;
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

            [TestCase("var temp1 = value", "")]
            [TestCase("var temp2 = value", "Assign(ref value)")]
            public void RefParameter(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        int value;
        var temp1 = value;
        Assign(ref value);
        var temp2 = value;
    }

    internal void Assign(ref int value)
    {
        value = 1;
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

            [TestCase("var temp1 = value;", "")]
            [TestCase("var temp2 = value;", "1")]
            public void NotInitialized(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        int value;
        var temp1 = value;
        value = 1;
        var temp2 = value;
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
            public void AssignedInLock()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandBox
{
    using System;

    public class Foo : IDisposable
    {
        private readonly object gate;

        public IDisposable disposable;
        private bool disposed;

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            var toDispose = (IDisposable)null;
            lock (this.gate)
            {
                if (this.disposed)
                {
                    return;
                }

                this.disposed = true;
                toDispose = this.disposable;
                this.disposable = null;
            }

            var temp = toDispose;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause("var temp = toDispose;").Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item.AssignedValues);
                    Assert.AreEqual("(IDisposable)null, this.disposable", actual);
                }
            }
        }
    }
}