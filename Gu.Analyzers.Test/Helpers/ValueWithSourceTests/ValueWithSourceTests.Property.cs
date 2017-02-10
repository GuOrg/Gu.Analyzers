namespace Gu.Analyzers.Test
{
    using System.Linq;
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;

    using NUnit.Framework;

    public partial class ValueWithSourceTests
    {
        public class Property
        {
            [Test]
            public void AutoPublicGetSet()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        var temp1 = Value;
    }

    internal void Bar()
    {
        var temp2 = Value;
    }

    public int Value { get; set; }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, Value PotentiallyInjected", actual);
                }
            }

            [Test]
            public void AutoPublicGetSetAssignedBefore()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        this.Value = 1;
        var temp1 = Value;
    }

    internal void Bar()
    {
        var temp2 = Value;
    }

    public int Value { get; set; }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, 1 Constant", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, Value PotentiallyInjected, 1 Constant", actual);
                }
            }

            [Test]
            public void AutoPublicGetSetAssignedAfter()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        var temp1 = this.Value;
        this.Value = 1;
    }

    internal void Bar()
    {
        var temp2 = this.Value;
    }

    public int Value { get; set; }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = this.Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.Value Member", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = this.Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.Value Member, this.Value PotentiallyInjected, 1 Constant", actual);
                }
            }

            [Test]
            public void AutoPublicGetSetInitialized()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        var temp1 = Value;
    }

    internal void Bar()
    {
        var temp2 = Value;
    }

    public int Value { get; set; } = 1;
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, 1 Constant", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, Value PotentiallyInjected, 1 Constant", actual);
                }
            }

            [Test]
            public void AutoPublicGetSetInitializedInBaseCtor()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase
{
    internal FooBase()
    {
        this.Value = 1;
    }

    public int Value { get; set; }
}

internal class Foo : FooBase
{
    internal Foo()
    {
        var temp1 = Value;
    }

    internal void Bar()
    {
        var temp2 = Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, 1 Constant", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, Value PotentiallyInjected, 1 Constant", actual);
                }
            }

            [Test]
            public void AutoPublicGetSetInitializedInBaseCtorWhenBaseHasManyCtors()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase
{
    internal FooBase()
    {
        this.Value = 1;
    }

    internal FooBase(int value)
    {
        this.Value = value;
    }

    public int Value { get; set; }
}

internal class Foo : FooBase
{
    internal Foo()
    {
        var temp1 = Value;
    }

    internal void Bar()
    {
        var temp2 = Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, 1 Constant", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, Value PotentiallyInjected, 1 Constant", actual);
                }
            }

            [Test]
            public void AutoPublicGetSetInjectedInBaseCtorWhenBaseHasManyCtors()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase
{
    internal FooBase()
    {
        this.Value = 1;
    }

    internal FooBase(int value)
    {
        this.Value = value;
    }

    public int Value { get; set; }
}

internal class Foo : FooBase
{
    internal Foo(int arg)
        : base(arg)
    {
        var temp1 = Value;
    }

    internal void Bar()
    {
        var temp2 = Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, value Argument, arg Injected", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, Value PotentiallyInjected, value Argument, arg Injected", actual);
                }
            }

            [Test]
            public void AutoPublicGetSetInitializedInBaseCtorExplicitBaseCall()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase
{
    internal FooBase()
    {
        this.Value = 1;
    }

    public int Value { get; set; }
}

internal class Foo : FooBase
{
    internal Foo()
        : base()
    {
        var temp1 = Value;
    }

    internal void Bar()
    {
        var temp2 = Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, 1 Constant", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, Value PotentiallyInjected, 1 Constant", actual);
                }
            }

            [Test]
            public void AutoPublicGetSetInitializedInPreviousCtor()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo(int value)
    {
        this.Value = value;
    }

    internal Foo(string text)
        : this(1)
    {
        var temp1 = Value;
    }

    public int Value { get; set; }

    internal void Bar()
    {
        var temp2 = Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, 1 Constant", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, 1 Constant, Value PotentiallyInjected", actual);
                }
            }

            [Test]
            public void AutoPublicGetPrivateSet()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foor()
    {
        var temp1 = Value;
    }

    internal void Bar()
    {
        var temp2 = Value;
    }

    public int Value { get; private set; }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member", actual);
                }
            }

            [Test]
            public void AutoGetSetInitialized()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        var temp1 = Value;
    }

    internal void Bar()
    {
        var temp2 = Value;
    }

    public int Value { get; set; } = 1;
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, 1 Constant", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, Value PotentiallyInjected, 1 Constant", actual);
                }
            }

            [Test]
            public void GetPublicSetWithBackingFieldAssignedWithInjectedAndInializer()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    private int value = 1;

    public Foo(int ctorValue)
    {
        this.value = ctorValue;
        var temp1 = this.Value;
    }

    public int Value
    {
        get { return this.value; }
        set { this.value = value; }
    }

    public void Meh()
    {
        var temp2 = this.Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);

                var node = syntaxTree.EqualsValueClause("var temp1 = this.Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.Value Calculated, this.value Member, 1 Constant, ctorValue Injected", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = this.Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.Value Calculated, this.value Member, 1 Constant, ctorValue Injected, value Injected", actual);
                }
            }

            [Test]
            public void GetPrivateSetWithBackingFieldAssignedInCtorAndInializer1()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.IO;

public sealed class Foo
{
    private Stream stream = File.OpenRead(""A"");

    public Foo()
    {
        this.stream = File.OpenRead(""B"");
        var temp1 = this.stream;
    }

    public Stream Stream
    {
        get { return this.stream; }
        private set { this.stream = value; }
    }

    public void Bar()
    {
        var temp2 = this.stream;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = this.stream;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(@"this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = this.stream;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(@"this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External", actual);
                }
            }

            [Test]
            public void GetPublicSetWithBackingFieldAssignedInCtorAndInializer()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.IO;

public sealed class Foo
{
    private Stream stream = File.OpenRead(""A"");

    public Foo()
    {
        this.stream = File.OpenRead(""B"");
        var temp1 = this.Stream;
    }

    public Stream Stream
    {
        get { return this.stream; }
        set { this.stream = value; }
    }

    public void Bar()
    {
        var temp2 = this.stream;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = this.stream;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(@"stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = this.stream;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(@"this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External", actual);
                }
            }

            [Test]
            public void GetOnlyAssignedInCtorAndInializer()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.IO;

public sealed class Foo
{
    public Foo()
    {
        this.Stream = File.OpenRead(""A"");
        var temp1 = this.Stream;
    }

    public Stream Stream { get; } = File.OpenRead(""B"");

    public void Bar()
    {
        var temp2 = this.Stream;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = this.Stream;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(@"this.Stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = this.Stream;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(@"this.Stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External", actual);
                }
            }

            [Test]
            public void CalculatedReturningPrivateReadonlyFieldExpressionBody()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.IO;

public sealed class Foo
{
    private readonly FileStream stream = File.OpenRead(""A"");

    public Foo()
    {
        this.stream = File.OpenRead(""B"");
        var temp1 = this.Stream;
    }

    public Stream Stream => this.stream;

    public void Bar()
    {
        var temp2 = this.Stream;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = this.Stream;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(@"this.Stream Calculated, this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = this.Stream;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(@"this.Stream Calculated, this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External", actual);
                }
            }

            [Test]
            public void CalculatedReturningPublicFieldExpressionBody()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.IO;

public sealed class Foo
{
    public FileStream stream = File.OpenRead(""A"");

    public Foo()
    {
        this.stream = File.OpenRead(""B"");
        var temp1 = this.Stream;
    }

    public Stream Stream => this.stream;

    public void Bar()
    {
        var temp2 = this.Stream;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = this.Stream;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(@"this.Stream Calculated, this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = this.Stream;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(@"this.Stream Calculated, this.stream Member, this.stream PotentiallyInjected, File.OpenRead(""A"") External, File.OpenRead(""B"") External", actual);
                }
            }

            [Test]
            public void CalculatedReturningFieldStatementBody()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.IO;

public sealed class Foo
{
    private readonly FileStream stream = File.OpenRead(""A"");

    public Foo()
    {
        this.stream = File.OpenRead(""B"");
        var temp1 = this.Stream;
    }

    public Stream Stream
    {
        get
        {
            return this.stream;;
        }
    }

    public void Bar()
    {
        var temp2 = this.Stream;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = this.Stream;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(@"this.Stream Calculated, this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = this.Stream;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(@"this.Stream Calculated, this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External", actual);
                }
            }

            [Test]
            public void CalculatedStatementBodyReturningConstant()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
internal class Foo
{
    internal Foo()
    {
        var temp1 = this.Value;
    }

    public int Value
    {
        get
        {
            return 1;
        }
    }

    internal void Bar()
    {
        var temp2 = this.Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = this.Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.Value Calculated, 1 Constant", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = this.Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.Value Calculated, 1 Constant", actual);
                }
            }

            [Test]
            public void CalculatedExpressionBody()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
internal class Foo
{
    internal Foo()
    {
        var temp1 = this.Value;
    }

    public int Value => 1;

    internal void Bar()
    {
        var temp2 = this.Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = this.Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.Value Calculated, 1 Constant", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = this.Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.Value Calculated, 1 Constant", actual);
                }
            }

            [Test]
            public void CalculatedStatementBodyReturningField()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
internal class Foo
{
    private readonly int value = 1;

    internal Foo()
    {
        var temp1 = this.Value;
    }

    public int Value
    {
        get
        {
            return this.value;
        }
    }

    internal void Bar()
    {
        var temp2 = this.Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = this.Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.Value Calculated, this.value Member, 1 Constant", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = this.Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.Value Calculated, this.value Member, 1 Constant", actual);
                }
            }

            [Test]
            public void CalculatedReturningFieldExpressionBody()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
internal class Foo
{
    private readonly int value = 1;

    internal Foo()
    {
        var temp1 = this.Value;
    }

    public int Value => this.value;

    internal void Bar()
    {
        var temp2 = this.Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = this.Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.Value Calculated, this.value Member, 1 Constant", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = this.Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.Value Calculated, this.value Member, 1 Constant", actual);
                }
            }


            [TestCase("{ 1, 2, 3 }")]
            [TestCase("new [] { 1, 2, 3 }")]
            [TestCase("new int[] { 1, 2, 3 }")]
            public void PublicReadonlyArrayInitializedArrayThenAccessedWithIndexer(string collection)
            {
                var testCode = @"
internal class Foo
{
    internal Foo()
    {
        var temp1 = this.Array[1];
    }

    public int[] Array { get; } = { 1, 2, 3 };

    internal void Bar()
    {
        var temp2 = this.Array[1];
    }
}";
                testCode = testCode.AssertReplace("{ 1, 2, 3 }", collection);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = this.Array[1];").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"this.Array[1] Member, {collection} Created", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = this.Array[1];").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"this.Array[1] Member, this.Array[1] PotentiallyInjected, {collection} Created", actual);
                }
            }

            [Test]
            public void PublicReadonlyThenAccessedMutableNested()
            {
                var testCode = @"
internal class Nested
{
    public int Value;
}

internal class Foo
{
    internal Foo()
    {
        var temp1 = this.Nested.Value;
    }

    public Nested Nested { get; } = new Nested();

    internal void Bar()
    {
        var temp2 = this.Nested.Value;
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = this.Nested.Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"this.Nested.Value Member, this.Nested Created", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = this.Nested.Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"this.Nested.Value Member, this.Nested Member, new Nested() Created, this.Nested.Value PotentiallyInjected", actual);
                }
            }
        }
    }
}