namespace Gu.Analyzers.Test.Helpers
{
    using System.Linq;
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;

    using NUnit.Framework;

    public partial class ValueWithSourceTests
    {
        public class Field
        {
            [TestCase("private void Assign")]
            [TestCase("public void Assign")]
            [TestCase("public static void Assign")]
            public void PrivateAssignedWithOutParameterBeforeInCtor(string code)
            {
                var testCode = @"
internal class Foo
{
    private int field;

    public Foo()
    {
        this.Assign(out this.field);
        var temp1 = this.field;
    }

    internal void Bar()
    {
        var temp2 = this.field;
    }

    private void Assign(out int value)
    {
        value = 1;
    }
}";
                testCode = testCode.AssertReplace("private void Assign", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = this.field;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.field Member, this.Assign(out this.field) Out, 1 Constant", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = this.field;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.field Member, this.Assign(out this.field) Out, 1 Constant", actual);
                }
            }

            [TestCase("private void Assign")]
            [TestCase("public void Assign")]
            [TestCase("public static void Assign")]
            public void PrivateAssignedWithOutParameterAfterInCtor(string code)
            {
                var testCode = @"
internal class Foo
{
    private int field;

    internal Foo()
    {
        var temp1 = this.field;
        this.Assign(out this.field);
    }

    internal void Bar()
    {
        var temp2 = this.field;
    }

    private void Assign(out int value)
    {
        value = 1;
    }
}";
                testCode = testCode.AssertReplace("private void Assign", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = this.field;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.field Member", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = this.field;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.field Member, this.Assign(out this.field) Out, 1 Constant", actual);
                }
            }

            [TestCase("private void Assign")]
            [TestCase("public void Assign")]
            [TestCase("public static void Assign")]
            public void PrivateAssignedWithRefParameterAfter(string code)
            {
                var testCode = @"
internal class Foo
{
    private int field;

    internal Foo()
    {
        var temp1 = this.field;
        Assign(ref this.field);
    }

    internal void Bar()
    {
        var temp2 = this.field;
    }

    private void Assign(ref int value)
    {
        value = 1;
    }
}";
                testCode = testCode.AssertReplace("private void Assign", code);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = this.field;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.field Member", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = this.field;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.field Member, Assign(ref this.field) Ref, 1 Constant", actual);
                }
            }

            [Test]
            public void PublicInitialized()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    public int field = 1;

    internal Foo()
    {
        var temp1 = this.field;
    }

    internal void Bar()
    {
        var temp2 = this.field;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = this.field;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.field Member, 1 Constant", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = this.field;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.field Member, this.field PotentiallyInjected, 1 Constant", actual);
                }
            }

            [TestCase("{ 1, 2, 3 }")]
            [TestCase("new [] { 1, 2, 3 }")]
            [TestCase("new int[] { 1, 2, 3 }")]
            public void PublicArrayInitializedArrayThenAccessedWithIndexer(string collection)
            {
                var testCode = @"
internal class Foo
{
    public int[] field = { 1, 2, 3 };

    internal Foo()
    {
        var temp1 = this.field[1];
    }

    internal void Bar()
    {
        var temp2 = this.field[1];
    }
}";
                testCode = testCode.AssertReplace("{ 1, 2, 3 }", collection);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = this.field[1];").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"this.field[1] Member, {collection} Created", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = this.field[1];").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"this.field[1] Member, this.field[1] PotentiallyInjected, {collection} Created", actual);
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
    public readonly int[] field = { 1, 2, 3 };

    internal Foo()
    {
        var temp1 = this.field[1];
    }

    internal void Bar()
    {
        var temp2 = this.field[1];
    }
}";
                testCode = testCode.AssertReplace("{ 1, 2, 3 }", collection);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = this.field[1];").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"this.field[1] Member, {collection} Created", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = this.field[1];").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"this.field[1] Member, this.field[1] PotentiallyInjected, {collection} Created", actual);
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
    public readonly Nested field = new Nested();

    internal Foo()
    {
        var temp1 = this.field.Value;
    }

    internal void Bar()
    {
        var temp2 = this.field.Value;
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = this.field.Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"this.field.Value Member, this.field Member, new Nested() Created", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = this.field.Value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"this.field.Value Member, this.field.Value PotentiallyInjected, this.field Member, new Nested() Created", actual);
                }
            }

            [TestCase("public readonly")]
            [TestCase("private readonly")]
            [TestCase("private")]
            public void Initialized(string modifiers)
            {
                var testCode = @"
internal class Foo
{
    public readonly int field = 1;

    internal Foo()
    {
        var temp1 = this.field;
    }

    internal void Bar()
    {
        var temp2 = this.field;
    }
}";
                testCode = testCode.AssertReplace("public readonly", modifiers);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = this.field;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.field Member, 1 Constant", actual);
                }

                node = syntaxTree.EqualsValueClause("var temp2 = this.field;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.field Member, 1 Constant", actual);
                }
            }
        }
    }
}