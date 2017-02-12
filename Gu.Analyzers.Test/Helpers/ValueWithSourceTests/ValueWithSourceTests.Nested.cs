namespace Gu.Analyzers.Test.Helpers
{
    using System.Linq;
    using System.Threading;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    internal partial class ValueWithSourceTests
    {
        internal class Nested
        {
            [TestCase("var temp1 = this.nested.value;", "this.nested.value Member, this.nested Member, new Nested() Created")]
            [TestCase("var temp2 = this.Nested.Value;", "this.Nested.Value Member, this.Nested Member, new Nested() Created")]
            [TestCase("var temp3 = this.nested.value;", "this.nested.value Member, this.nested.value PotentiallyInjected, this.nested Member, new Nested() Created")]
            [TestCase("var temp4 = this.Nested.Value;", "this.Nested.Value Member, this.Nested.Value PotentiallyInjected, this.Nested Member, new Nested() Created")]
            public void PublicReadonlyThenAccessedMutableNested(string code, string expected)
            {
                var testCode = @"
public class Nested
{
    public int value;
    public int Value { get; set; }
}

internal class Foo
{
    public readonly Nested nested = new Nested();

    internal Foo()
    {
        var temp1 = this.nested.value;
        var temp2 = this.Nested.Value;
    }

    public Nested Nested { get; } = new Nested();

    internal void Bar()
    {
        var temp3 = this.nested.value;
        var temp4 = this.Nested.Value;
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.nested.value;", "this.nested.value Member, this.nested Member, new Nested() Created")]
            [TestCase("var temp2 = this.Nested.Value;", "this.Nested.Value Member, this.Nested Member, new Nested() Created")]
            [TestCase("var temp3 = this.nested.value;", "this.nested.value Member, this.nested Member, new Nested() Created")]
            [TestCase("var temp4 = this.Nested.Value;", "this.Nested.Value Member, this.Nested Member, new Nested() Created")]
            public void PrivateReadonlyThenAccessedMutableNested(string code, string expected)
            {
                var testCode = @"
public class Nested
{
    public int value;
    public int Value { get; set; }
}

internal class Foo
{
    private readonly Nested nested = new Nested();

    internal Foo()
    {
        var temp1 = this.nested.value;
        var temp2 = this.Nested.Value;
    }

    private Nested Nested { get; } = new Nested();

    internal void Bar()
    {
        var temp3 = this.nested.value;
        var temp4 = this.Nested.Value;
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
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