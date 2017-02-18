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

            [Test]
            public void OutParameter()
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
            public void ChainedOutParameter()
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
            public void RefParameter()
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
            public void NotInitialized()
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