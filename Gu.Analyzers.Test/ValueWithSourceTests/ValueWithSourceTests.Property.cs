namespace Gu.Analyzers.Test
{
    using System.Linq;
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

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

    internal static void Bar()
    {
        var temp2 = Value;
    }

    public int Value { get; set; }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
                Assert.AreEqual("var temp1 = Value;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member", actual);
                }

                node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
                Assert.AreEqual("var temp2 = Value;", node.FirstAncestor<StatementSyntax>().ToString());
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

    internal static void Bar()
    {
        var temp2 = Value;
    }

    public int Value { get; set; }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
                Assert.AreEqual("var temp1 = Value;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, 1 Constant", actual);
                }

                node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
                Assert.AreEqual("var temp2 = Value;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, 1 Constant, Value PotentiallyInjected", actual);
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
        var temp1 = Value;
        this.Value = 1;
    }

    internal static void Bar()
    {
        var temp2 = Value;
    }

    public int Value { get; set; }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
                Assert.AreEqual("var temp1 = Value;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member", actual);
                }

                node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
                Assert.AreEqual("var temp2 = Value;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, 1 Constant, Value PotentiallyInjected", actual);
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

    internal static void Bar()
    {
        var temp2 = Value;
    }

    public int Value { get; set; } = 1;
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
                Assert.AreEqual("var temp1 = Value;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, 1 Constant", actual);
                }

                node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
                Assert.AreEqual("var temp2 = Value;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, 1 Constant, Value PotentiallyInjected", actual);
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

    internal static void Bar()
    {
        var temp2 = Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
                Assert.AreEqual("var temp1 = Value;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, 1 Constant", actual);
                }

                node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
                Assert.AreEqual("var temp2 = Value;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, 1 Constant, Value PotentiallyInjected", actual);
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
        this.Value = Value;
    }

    internal Foo(string text)
        : this(text.Length)
    {
        var temp1 = Value;
    }

    public int Value { get; set; }

    internal static void Bar()
    {
        var temp2 = Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
                Assert.AreEqual("var temp1 = Value;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, 1 Constant", actual);
                }

                node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
                Assert.AreEqual("var temp2 = Value;", node.FirstAncestor<StatementSyntax>().ToString());
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
    internal static void Bar()
    {
        var value = Value;
    }

    public int Value { get; private set; }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("Value");
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
    internal static void Bar()
    {
        var value = Value;
    }

    public int Value { get; set; } = 1;
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("Value");
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Member, Value PotentiallyInjected, 1 Constant", actual);
                }
            }

            [Test]
            public void GetPublicSetWithBackingFieldAssignedWithInjectedAndInializerInCtor()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    private int value = 1;

    public Foo(int ctorValue)
    {
        this.value = ctorValue;
        var meh = this.Value;
    }

    public int Value
    {
        get { return this.value; }
        set { this.value = value; }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("Value");
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Calculated, this.value Member, 1 Constant, ctorValue Injected", actual);
                }
            }

            [Test]
            public void GetPublicSetWithBackingFieldAssignedWithInjectedAndInializerInMethod()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    private int value = 1;

    public Foo(int ctorValue)
    {
        this.value = ctorValue;
    }

    public int Value
    {
        get { return this.value; }
        set { this.value = value; }
    }

    public void Meh()
    {
        var meh = this.Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("meh");
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Calculated, this.value Member, 1 Constant, ctorValue Injected, value Injected", actual);
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
        var temp = this.stream;
    }

    public Stream Stream
    {
        get { return this.stream; }
        private set { this.stream = value; }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
                Assert.AreEqual("var temp = this.stream;", node.FirstAncestor<StatementSyntax>().ToString());
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
        var temp = this.Stream;
    }

    public Stream Stream
    {
        get { return this.stream; }
        set { this.stream = value; }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
                Assert.AreEqual("var temp = this.stream;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(@"stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External", actual);
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
        var temp = this.Stream;
    }

    public Stream Stream { get; } = File.OpenRead(""B"");
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("Stream");
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(@"Stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External", actual);
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
        var temp = this.Stream;
    }

    public Stream Stream => this.stream;
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("Stream");
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(@"Stream Calculated, this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External", actual);
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
        var temp = this.Stream;
    }

    public Stream Stream => this.stream;
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("Stream");
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(@"Stream Calculated, this.stream Member, this.stream PotentiallyInjected, File.OpenRead(""A"") External, File.OpenRead(""B"") External", actual);
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
        var temp = this.Stream;
    }

    public Stream Stream
    {
        get
        {
            return this.stream;;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("Stream");
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(@"Stream Calculated, this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External", actual);
                }
            }

            [Test]
            public void CalculatedStatementBodyReturningConstant()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
internal class Foo
{
    internal static void Bar()
    {
        var value = Value;
    }

    public int Value
    {
        get
        {
            return 1;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("Value");
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Calculated, 1 Constant", actual);
                }
            }

            [Test]
            public void CalculatedExpressionBody()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
internal class Foo
{
    internal static void Bar()
    {
        var value = Value;
    }

    public int Value => 1;
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("Value");
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Calculated, 1 Constant", actual);
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

    internal static void Bar()
    {
        var value = Value;
    }

    public int Value
    {
        get
        {
            return this.value;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("Value");
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Value Calculated, this.value Member, 1 Constant", actual);
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

    internal static void Bar()
    {
        var value = this.Value;
    }

    public int Value => this.value;
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.Value Calculated, this.value Member, 1 Constant", actual);
                }
            }
        }
    }
}