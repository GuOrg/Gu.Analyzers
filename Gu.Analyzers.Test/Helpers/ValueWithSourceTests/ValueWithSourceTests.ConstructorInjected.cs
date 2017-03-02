namespace Gu.Analyzers.Test.Helpers
{
    using System.Linq;
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;

    using NUnit.Framework;

    internal partial class ValueWithSourceTests
    {
        public class ConstructorInjected
        {
            [TestCase("var temp = meh;", "meh PotentiallyInjected, meh Argument, 1 Constant, 2 Constant, text.Length External, text Injected")]
            public void ChainedDefaultValue(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandBox
{
    internal class Foo
    {
        public Foo()
            : this(1)
        {
        }

        public Foo(double gg)
            : this(1, 2)
        {
        }

        public Foo(string text)
            : this(1, text.Length)
        {
        }

        public Foo(int _, int meh = 1)
        {
            var temp = meh;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [Test]
            public void PrivateInjectedFactoryConstant()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
public sealed class Foo
{
    private Foo(int value)
    {
        var temp = value;
    }

    public static Foo Create()
    {
        return new Foo(1);
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp = value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("value Argument, 1 Constant", actual);
                }
            }

            [Test]
            public void PrivateInjectedFactoryConstantGeneric()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
public sealed class Foo<T>
{
    private Foo(T value)
    {
        var temp = value;
    }

    public static Foo Create()
    {
        return new Foo(default(T));
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp = value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("value Argument, default(T) Constant", actual);
                }
            }

            [Test]
            public void PrivateInjectedFactoryCached()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;

public sealed class Foo
{
    private static readonly IDisposable CachedDisposable = new Disposable();

    private Foo(IDisposable value)
    {
        var temp = value;
    }

    public static Foo Create() => new Foo(CachedDisposable);
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp = value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("value Argument, CachedDisposable Cached", actual);
                }
            }

            [Test]
            public void PrivateInjectedFactoryConst()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;

public sealed class Foo
{
    private const int ConstValue = 1;

    private Foo(int value)
    {
        var temp = value;
    }

    public static Foo Create() => new Foo(ConstValue);
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp = value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("value Argument, ConstValue Cached", actual);
                }
            }

            [TestCase("var temp1 = this.value;", "this.value Member")]
            [TestCase("var temp2 = this.Value;", "this.Value Member")]
            [TestCase("var temp3 = this.value;", "this.value Member, ctorArg PotentiallyInjected, ctorArg Argument, 1 Constant")]
            [TestCase("var temp4 = this.Value;", "this.Value Member, ctorArg PotentiallyInjected, ctorArg Argument, 1 Constant")]
            [TestCase("var temp5 = this.value;", "this.value Member, ctorArg Argument, 1 Constant")]
            [TestCase("var temp6 = this.Value;", "this.Value Member, ctorArg Argument, 1 Constant")]
            [TestCase("var temp7 = this.value;", "this.value Member, this.value PotentiallyInjected, ctorArg PotentiallyInjected, ctorArg Argument, 1 Constant")]
            [TestCase("var temp8 = this.Value;", "this.Value Member, this.Value PotentiallyInjected, ctorArg PotentiallyInjected, ctorArg Argument, 1 Constant")]
            public void MutableInitializedInPreviousCtor(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    public int value;

    internal Foo(int ctorArg)
    {
        var temp1 = this.value;
        var temp2 = this.Value;
        this.value = ctorArg;
        this.Value = ctorArg;
        var temp3 = this.value;
        var temp4 = this.Value;
    }

    internal Foo(string text)
        : this(1)
    {
        var temp5 = this.value;
        var temp6 = this.Value;
    }

    public int Value { get; set; }

    internal void Bar()
    {
        var temp7 = this.value;
        var temp8 = this.Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "this.value Member")]
            [TestCase("var temp2 = this.Value;", "this.Value Member")]
            [TestCase("var temp3 = this.value;", "this.value Member, ctorArg PotentiallyInjected, ctorArg Argument, default(T) Constant")]
            [TestCase("var temp4 = this.Value;", "this.Value Member, ctorArg PotentiallyInjected, ctorArg Argument, default(T) Constant")]
            [TestCase("var temp5 = this.value;", "this.value Member, ctorArg Argument, default(T) Constant")]
            [TestCase("var temp6 = this.Value;", "this.Value Member, ctorArg Argument, default(T) Constant")]
            [TestCase("var temp7 = this.value;", "this.value Member, this.value PotentiallyInjected, ctorArg PotentiallyInjected, ctorArg Argument, default(T) Constant")]
            [TestCase("var temp8 = this.Value;", "this.Value Member, this.Value PotentiallyInjected, ctorArg PotentiallyInjected, ctorArg Argument, default(T) Constant")]
            public void MutableInitializedInPreviousCtorGeneric(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo<T>
{
    public int value;

    internal Foo(T ctorArg)
    {
        var temp1 = this.value;
        var temp2 = this.Value;
        this.value = ctorArg;
        this.Value = ctorArg;
        var temp3 = this.value;
        var temp4 = this.Value;
    }

    internal Foo(string text)
        : this(default(T))
    {
        var temp5 = this.value;
        var temp6 = this.Value;
    }

    public T Value { get; set; }

    internal void Bar()
    {
        var temp7 = this.value;
        var temp8 = this.Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "this.value Member")]
            [TestCase("var temp2 = this.Value;", "this.Value Member")]
            [TestCase("var temp3 = this.value;", "this.value Member, baseArg Injected")]
            [TestCase("var temp4 = this.Value;", "this.Value Member, baseArg Injected")]
            [TestCase("var temp5 = this.value;", "this.value Member, baseArg Argument, arg Injected")]
            [TestCase("var temp6 = this.Value;", "this.Value Member, baseArg Argument, arg Injected")]
            [TestCase("var temp7 = this.value;", "this.value Member, baseArg Argument, 2 Constant")]
            [TestCase("var temp8 = this.Value;", "this.Value Member, baseArg Argument, 2 Constant")]
            [TestCase("var temp9 = this.value;", "this.value Member, this.value PotentiallyInjected, baseArg Argument, arg Injected, 2 Constant")]
            [TestCase("var temp10 = this.Value;", "this.Value Member, this.Value PotentiallyInjected, baseArg Argument, arg Injected, 2 Constant")]
            public void MutableInBaseInjectedInBaseCtorWhenBaseHasManyCtors(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase
{
    public int value;

    internal FooBase()
    {
        this.value = 1;
        this.Value = 1;
    }

    internal FooBase(int baseArg)
    {
        var temp1 = this.value;
        var temp2 = this.Value;
        this.value = baseArg;
        this.Value = baseArg;
        var temp3 = this.value;
        var temp4 = this.Value;
    }

    public int Value { get; set; }
}

internal class Foo : FooBase
{
    internal Foo(int arg)
        : base(arg)
    {
        var temp5 = this.value;
        var temp6 = this.Value;
    }

    internal Foo()
        : base(2)
    {
        var temp7 = this.value;
        var temp8 = this.Value;
    }

    internal void Bar()
    {
        var temp9 = this.value;
        var temp10 = this.Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "this.value Member, baseArg Injected")]
            [TestCase("var temp2 = this.Value;", "this.Value Member, baseArg Injected")]
            [TestCase("var temp3 = this.value;", "this.value Member, baseArg Argument, arg Injected")]
            [TestCase("var temp4 = this.Value;", "this.Value Member, baseArg Argument, arg Injected")]
            [TestCase("var temp5 = this.value;", "this.value Member, baseArg Argument, 2 Constant")]
            [TestCase("var temp6 = this.Value;", "this.Value Member, baseArg Argument, 2 Constant")]
            [TestCase("var temp7 = this.value;", "this.value Member, this.value PotentiallyInjected, baseArg Argument, arg Injected, 2 Constant")]
            [TestCase("var temp8 = this.Value;", "this.Value Member, this.Value PotentiallyInjected, baseArg Argument, arg Injected, 2 Constant")]
            public void MutableInBaseInjectedInBaseCtorWhenBaseHasManyCtorsGeneric(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase<T>
{
    public T value;

    internal FooBase()
    {
        this.value = default(T);
        this.Value = default(T);
    }

    internal FooBase(T baseArg)
    {
        this.value = baseArg;
        this.Value = baseArg;
        var temp1 = this.value;
        var temp2 = this.Value;
    }

    public T Value { get; set; }
}

internal class Foo : FooBase<int>
{
    internal Foo(int arg)
        : base(arg)
    {
        var temp3 = this.value;
        var temp4 = this.Value;
    }

    internal Foo()
        : base(2)
    {
        var temp5 = this.value;
        var temp6 = this.Value;
    }

    internal void Bar()
    {
        var temp7 = this.value;
        var temp8 = this.Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "this.value Member, baseArg Injected")]
            [TestCase("var temp2 = this.Value;", "this.Value Member, baseArg Injected")]
            [TestCase("var temp3 = this.value;", "this.value Member, baseArg Argument, arg Injected")]
            [TestCase("var temp4 = this.Value;", "this.Value Member, baseArg Argument, arg Injected")]
            [TestCase("var temp5 = this.value;", "this.value Member, baseArg Argument, default(T) Constant")]
            [TestCase("var temp6 = this.Value;", "this.Value Member, baseArg Argument, default(T) Constant")]
            [TestCase("var temp7 = this.value;", "this.value Member, this.value PotentiallyInjected, baseArg Argument, arg Injected, default(T) Constant")]
            [TestCase("var temp8 = this.Value;", "this.Value Member, this.Value PotentiallyInjected, baseArg Argument, arg Injected, default(T) Constant")]
            public void MutableInBaseInjectedInBaseCtorWhenBaseHasManyCtorsGenericGeneric(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase<T>
{
    public T value;

    internal FooBase()
    {
        this.value = default(T);
        this.Value = default(T);
    }

    internal FooBase(T baseArg)
    {
        this.value = baseArg;
        this.Value = baseArg;
        var temp1 = this.value;
        var temp2 = this.Value;
    }

    public T Value { get; set; }
}

internal class Foo<T> : FooBase<T>
{
    internal Foo(T arg)
        : base(arg)
    {
        var temp3 = this.value;
        var temp4 = this.Value;
    }

    internal Foo()
        : base(default(T))
    {
        var temp5 = this.value;
        var temp6 = this.Value;
    }

    internal void Bar()
    {
        var temp7 = this.value;
        var temp8 = this.Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "this.value Member, 1 Constant")]
            [TestCase("var temp2 = this.Value;", "this.Value Member, 1 Constant")]
            [TestCase("var temp3 = this.value;", "this.value Member, this.value PotentiallyInjected, 1 Constant")]
            [TestCase("var temp4 = this.Value;", "this.Value Member, this.Value PotentiallyInjected, 1 Constant")]
            public void MutableInBaseInitializedInBaseCtorWhenBaseHasManyCtorsImplicitBaseCall(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase
{
    public int value;

    internal FooBase()
    {
        this.value = 1;
        this.Value = 1;
    }

    internal FooBase(int value)
    {
        this.value = value;
        this.Value = value;
    }

    public int Value { get; set; }
}

internal class Foo : FooBase
{
    internal Foo()
    {
        var temp1 = this.value;
        var temp2 = this.Value;
    }

    internal void Bar()
    {
        var temp3 = this.value;
        var temp4 = this.Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "this.value Member, default(T) Constant")]
            [TestCase("var temp2 = this.Value;", "this.Value Member, default(T) Constant")]
            [TestCase("var temp3 = this.value;", "this.value Member, this.value PotentiallyInjected, default(T) Constant")]
            [TestCase("var temp4 = this.Value;", "this.Value Member, this.Value PotentiallyInjected, default(T) Constant")]
            public void MutableInBaseInitializedInBaseCtorWhenBaseHasManyCtorsImplicitBaseCallGeneric(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase<T>
{
    public T value;

    internal FooBase()
    {
        this.value = default(T);
        this.Value = default(T);
    }

    internal FooBase(T value)
    {
        this.value = value;
        this.Value = value;
    }

    public T Value { get; set; }
}

internal class Foo : FooBase<int>
{
    internal Foo()
    {
        var temp1 = this.value;
        var temp2 = this.Value;
    }

    internal void Bar()
    {
        var temp3 = this.value;
        var temp4 = this.Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "this.value Member, default(T) Constant")]
            [TestCase("var temp2 = this.Value;", "this.Value Member, default(T) Constant")]
            [TestCase("var temp3 = this.value;", "this.value Member, this.value PotentiallyInjected, default(T) Constant")]
            [TestCase("var temp4 = this.Value;", "this.Value Member, this.Value PotentiallyInjected, default(T) Constant")]
            public void MutableInBaseInitializedInBaseCtorWhenBaseHasManyCtorsImplicitBaseCallGenericGeneric(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase<T>
{
    public T value;

    internal FooBase()
    {
        this.value = default(T);
        this.Value = default(T);
    }

    internal FooBase(T value)
    {
        this.value = value;
        this.Value = value;
    }

    public T Value { get; set; }
}

internal class Foo<T> : FooBase<T>
{
    internal Foo()
    {
        var temp1 = this.value;
        var temp2 = this.Value;
    }

    internal void Bar()
    {
        var temp3 = this.value;
        var temp4 = this.Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "this.value Member, 1 Constant")]
            [TestCase("var temp2 = this.Value;", "this.Value Member, 1 Constant")]
            [TestCase("var temp3 = this.value;", "this.value Member, this.value PotentiallyInjected, 1 Constant")]
            [TestCase("var temp4 = this.Value;", "this.Value Member, this.Value PotentiallyInjected, 1 Constant")]
            public void MutableInBaseInitializedInBaseCtorWhenBaseHasManyCtorsExplicitBaseCall(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase
{
    public int value;

    internal FooBase()
    {
        this.value = 1;
        this.Value = 1;
    }

    internal FooBase(int value)
    {
        this.value = value;
        this.Value = value;
    }

    public int Value { get; set; }
}

internal class Foo : FooBase
{
    internal Foo()
        : base()
    {
        var temp1 = this.value;
        var temp2 = this.Value;
    }

    internal void Bar()
    {
        var temp3 = this.value;
        var temp4 = this.Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "this.value Member, default(T) Constant")]
            [TestCase("var temp2 = this.Value;", "this.Value Member, default(T) Constant")]
            [TestCase("var temp3 = this.value;", "this.value Member, this.value PotentiallyInjected, default(T) Constant")]
            [TestCase("var temp4 = this.Value;", "this.Value Member, this.Value PotentiallyInjected, default(T) Constant")]
            public void MutableInBaseInitializedInBaseCtorWhenBaseHasManyCtorsExplicitBaseCallGeneric(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase<T>
{
    public T value;

    internal FooBase()
    {
        this.value = default(T);
        this.Value = default(T);
    }

    internal FooBase(T value)
    {
        this.value = value;
        this.Value = value;
    }

    public T Value { get; set; }
}

internal class Foo : FooBase<int>
{
    internal Foo()
        : base()
    {
        var temp1 = this.value;
        var temp2 = this.Value;
    }

    internal void Bar()
    {
        var temp3 = this.value;
        var temp4 = this.Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "this.value Member, default(T) Constant")]
            [TestCase("var temp2 = this.Value;", "this.Value Member, default(T) Constant")]
            [TestCase("var temp3 = this.value;", "this.value Member, this.value PotentiallyInjected, default(T) Constant")]
            [TestCase("var temp4 = this.Value;", "this.Value Member, this.Value PotentiallyInjected, default(T) Constant")]
            public void MutableInBaseInitializedInBaseCtorWhenBaseHasManyCtorsExplicitBaseCallGenericGeneric(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase<T>
{
    public T value;

    internal FooBase()
    {
        this.value = default(T);
        this.Value = default(T);
    }

    internal FooBase(int value)
    {
        this.value = value;
        this.Value = value;
    }

    public T Value { get; set; }
}

internal class Foo<T> : FooBase<T>
{
    internal Foo()
        : base()
    {
        var temp1 = this.value;
        var temp2 = this.Value;
    }

    internal void Bar()
    {
        var temp3 = this.value;
        var temp4 = this.Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }
        }
    }
}