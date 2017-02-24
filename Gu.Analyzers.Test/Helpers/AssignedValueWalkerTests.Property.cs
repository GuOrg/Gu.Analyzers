namespace Gu.Analyzers.Test.Helpers
{
    using System.Linq;
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;

    using NUnit.Framework;

    internal partial class AssignedValueWalkerTests
    {
        [TestCase("var temp1 = this.bar;", "1", null, null)]
        [TestCase("var temp2 = this.Bar;", "this.bar", "this.bar", "this.bar, 1")]
        [TestCase("var temp3 = this.bar;", "1, 2", null, null)]
        [TestCase("var temp4 = this.Bar;", "this.bar", "this.bar", "this.bar, 1, 2")]
        [TestCase("var temp5 = this.bar;", "1, 2, value", "value", "1, 2, value, this.bar")]
        [TestCase("var temp6 = this.Bar;", "this.bar", "this.bar", "this.bar, 1, 2, value")]
        public void BackingFieldPrivateSetInitializedAndAssignedInCtor(string code1, string expected, string assigned, string expected2)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
public sealed class Foo
{
    private int bar = 1;

    public Foo()
    {
        var temp1 = this.bar;
        var temp2 = this.Bar;
        this.bar = 2;
        var temp3 = this.bar;
        var temp4 = this.Bar;
    }

    public int Bar
    {
        get { return this.bar; }
        private set { this.bar = value; }
    }

    public void Meh()
    {
        var temp5 = this.bar;
        var temp6 = this.Bar;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause(code1).Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues.Select(x => x.Value));
                Assert.AreEqual(expected, actual);

                if (assigned != null)
                {
                    var assignedValue = pooled.Item.AssignedValues.Single(x => x.Value.ToFullString().Contains(assigned));
                    pooled.Item.AppendAssignmentsFor(assignedValue.Value);
                    var actual2 = string.Join(", ", pooled.Item.AssignedValues.Select(x => x.Value));
                    Assert.AreEqual(expected2, actual2);
                }
            }
        }

        [TestCase("var temp1 = this.bar;", "1")]
        [TestCase("var temp2 = this.Bar;", "this.bar")]
        [TestCase("var temp3 = this.bar;", "1, 2")]
        [TestCase("var temp4 = this.Bar;", "this.bar")]
        [TestCase("var temp5 = this.bar;", "1, 2, value")]
        [TestCase("var temp6 = this.Bar;", "this.bar")]
        public void BackingFieldPublicSetInitializedAndAssignedInCtor(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
public sealed class Foo
{
    private int bar = 1;

    public Foo()
    {
        var temp1 = this.bar;
        var temp2 = this.Bar;
        this.bar = 2;
        var temp3 = this.bar;
        var temp4 = this.Bar;
    }

    public int Bar
    {
        get { return this.bar; }
        set { this.bar = value; }
    }

    public void Meh()
    {
        var temp5 = this.bar;
        var temp6 = this.Bar;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause(code).Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues.Select(x => x.Value));
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.bar;", "1", null, null)]
        [TestCase("var temp2 = this.Bar;", "this.bar", "this.bar", "this.bar, 1")]
        [TestCase("var temp3 = this.bar;", "1, value", "value", "1, value, this.bar, 2")]
        [TestCase("var temp4 = this.Bar;", "this.bar, 2", "this.bar", "this.bar, 2, 1, value")]
        [TestCase("var temp5 = this.bar;", "1, value", "value", "1, value, this.bar, 2")]
        [TestCase("var temp6 = this.Bar;", "this.bar, 2", "this.bar", "this.bar, 2, 1, value")]
        public void BackingFieldPublicSetInitializedAndPropertyAssignedInCtor(string code, string expected, string assigned, string expected2)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
public sealed class Foo
{
    private int bar = 1;

    public Foo()
    {
        var temp1 = this.bar;
        var temp2 = this.Bar;
        this.Bar = 2;
        var temp3 = this.bar;
        var temp4 = this.Bar;
    }

    public int Bar
    {
        get { return this.bar; }
        private set { this.bar = value; }
    }

    public void Meh()
    {
        var temp5 = this.bar;
        var temp6 = this.Bar;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause(code).Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.AssignedValues.Select(x => x.Value));
                Assert.AreEqual(expected, actual);

                if (assigned != null)
                {
                    var assignedValue = pooled.Item.AssignedValues.Single(x => x.Value.ToFullString().Contains(assigned));
                    pooled.Item.AppendAssignmentsFor(assignedValue.Value);
                    var actual2 = string.Join(", ", pooled.Item.AssignedValues.Select(x => x.Value));
                    Assert.AreEqual(expected2, actual2);
                }
            }
        }
    }
}