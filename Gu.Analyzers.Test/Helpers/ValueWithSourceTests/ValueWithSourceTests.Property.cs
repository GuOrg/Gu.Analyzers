namespace Gu.Analyzers.Test.Helpers
{
    using System.Linq;
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;

    using NUnit.Framework;

    internal partial class ValueWithSourceTests
    {
        public class Property
        {
            [TestCase("var temp1 = Value;", "Value Member")]
            [TestCase("var temp2 = Value;", "Value Member, Value PotentiallyInjected")]
            public void AutoPublicGetSet(string code, string expected)
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
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = Value;", "Value Member, 1 Constant")]
            [TestCase("var temp2 = Value;", "Value Member, Value PotentiallyInjected, 1 Constant")]
            public void AutoPublicGetSetAssignedBefore(string code, string expected)
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
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.Value;", "this.Value Member")]
            [TestCase("var temp2 = this.Value;", "this.Value Member, this.Value PotentiallyInjected, 1 Constant")]
            public void AutoPublicGetSetAssignedAfter(string code, string expected)
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
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = Value;", "Value Member, 1 Constant")]
            [TestCase("var temp2 = Value;", "Value Member, Value PotentiallyInjected, 1 Constant")]
            public void AutoPublicGetSetInitialized(string code, string expected)
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
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = Value;", "Value Member")]
            [TestCase("var temp2 = Value;", "Value Member")]
            public void AutoPublicGetPrivateSet(string code, string expected)
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

    public int Value { get; private set; }
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

            [TestCase("var temp1 = Value;", "Value Member, 1 Constant")]
            [TestCase("var temp2 = Value;", "Value Member, Value PotentiallyInjected, 1 Constant")]
            public void AutoGetSetInitialized(string code, string expected)
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
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "this.value Member, 1 Constant, ctorValue Injected")]
            [TestCase("var temp2 = this.Value;", "this.Value Calculated, this.value Member, 1 Constant, ctorValue Injected")]
            [TestCase("var temp3 = this.value;", "this.value Member, 1 Constant, ctorValue Injected, value PotentiallyInjected")]
            [TestCase("var temp4 = this.Value;", "this.Value Calculated, this.value Member, 1 Constant, ctorValue Injected, value PotentiallyInjected")]
            public void GetPublicSetWithBackingFieldAssignedWithInjectedAndInializer(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    private int value = 1;

    public Foo(int ctorValue)
    {
        this.value = ctorValue;
        var temp1 = this.value;
        var temp2 = this.Value;
    }

    public int Value
    {
        get { return this.value; }
        set { this.value = value; }
    }

    public void Meh()
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

            [TestCase("var temp1 = this.value;", "this.value Member, 1 Constant, ctorValue Injected")]
            [TestCase("var temp2 = this.Value;", "this.Value Calculated, this.value Member, 1 Constant, ctorValue Injected")]
            [TestCase("var temp3 = this.value;", "this.value Member, 1 Constant, ctorValue Injected, value PotentiallyInjected")]
            [TestCase("var temp4 = this.Value;", "this.Value Calculated, this.value Member, 1 Constant, ctorValue Injected, value PotentiallyInjected")]
            public void GetPublicSetWithBackingPropertyAssignedWithInjectedAndInializer(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    private int value = 1;

    public Foo(int ctorValue)
    {
        this.Value = ctorValue;
        var temp1 = this.value;
        var temp2 = this.Value;
    }

    public int Value
    {
        get { return this.value; }
        set { this.value = value; }
    }

    public void Meh()
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

            [TestCase("var temp1 = this.stream;", @"this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External")]
            [TestCase("var temp2 = this.Stream;", @"this.Stream Calculated, this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External")]
            [TestCase("var temp3 = this.stream;", @"this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External, value Argument")]
            [TestCase("var temp4 = this.Stream;", @"this.Stream Calculated, this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External")]
            public void GetPrivateSetWithBackingFieldAssignedInCtorAndInializer1(string code, string expected)
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
        var temp2 = this.Stream;
    }

    public Stream Stream
    {
        get { return this.stream; }
        private set { this.stream = value; }
    }

    public void Bar()
    {
        var temp3 = this.stream;
        var temp4 = this.Stream;
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

            [TestCase("var temp1 = this.stream;", @"this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External")]
            [TestCase("var temp2 = this.Stream;", @"this.Stream Calculated, this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External")]
            [TestCase("var temp3 = this.stream;", @"this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External, value PotentiallyInjected")]
            [TestCase("var temp4 = this.Stream;", @"this.Stream Calculated, this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External, value PotentiallyInjected")]
            public void GetPublicSetWithBackingFieldAssignedInCtorAndInializer(string code, string expected)
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
        var temp2 = this.Stream;
    }

    public Stream Stream
    {
        get { return this.stream; }
        set { this.stream = value; }
    }

    public void Bar()
    {
        var temp3 = this.stream;
        var temp4 = this.Stream;
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

            [TestCase("var temp1 = this.Stream;", @"this.Stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External")]
            [TestCase("var temp2 = this.Stream;", @"this.Stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External")]
            public void GetOnlyAssignedInCtorAndInializer(string code, string expected)
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
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.Stream;", @"this.Stream Calculated, this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External")]
            [TestCase("var temp1 = this.Stream;", @"this.Stream Calculated, this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External")]
            public void CalculatedReturningPrivateReadonlyFieldExpressionBody(string code, string expected)
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
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.Stream;", @"this.Stream Calculated, this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External")]
            [TestCase("var temp2 = this.Stream;", @"this.Stream Calculated, this.stream Member, this.stream PotentiallyInjected, File.OpenRead(""A"") External, File.OpenRead(""B"") External")]
            public void CalculatedReturningPublicFieldExpressionBody(string code, string expected)
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
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.Stream;", @"this.Stream Calculated, this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External")]
            [TestCase("var temp2 = this.Stream;", @"this.Stream Calculated, this.stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External")]
            public void CalculatedReturningFieldStatementBody(string code, string expected)
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
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.Value;", "this.Value Calculated, 1 Constant")]
            [TestCase("var temp2 = this.Value;", "this.Value Calculated, 1 Constant")]
            public void CalculatedStatementBodyReturningConstant(string code, string expected)
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
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.Value;", "this.Value Calculated, 1 Constant")]
            [TestCase("var temp2 = this.Value;", "this.Value Calculated, 1 Constant")]
            public void CalculatedExpressionBody(string code, string expected)
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
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.Value;", "this.Value Calculated, this.value Member, 1 Constant")]
            [TestCase("var temp2 = this.Value;", "this.Value Calculated, this.value Member, 1 Constant")]
            public void CalculatedStatementBodyReturningField(string code, string expected)
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
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.Value;", "this.Value Calculated, this.value Member, 1 Constant")]
            [TestCase("var temp2 = this.Value;", "this.Value Calculated, this.value Member, 1 Constant")]
            public void CalculatedReturningFieldExpressionBody(string code, string expected)
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