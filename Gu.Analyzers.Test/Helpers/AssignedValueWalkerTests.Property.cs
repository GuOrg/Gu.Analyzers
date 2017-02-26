namespace Gu.Analyzers.Test.Helpers
{
    using System.Linq;
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;

    using NUnit.Framework;

    internal partial class AssignedValueWalkerTests
    {
        [TestCase("var temp = this.Bar1;", "this.bar1")]
        [TestCase("var temp = this.Bar2;", "2")]
        [TestCase("var temp = this.Bar3;", "3")]
        public void Caclulated(string code, string expected)
        {
            var testCode = @"
namespace RoslynSandBox
{
    public sealed class Foo
    {
        private int bar1 = 1;

        public Foo()
        {
            var temp = this.Bar1;
        }

        public int Bar1 => this.bar1;

        public int Bar2 => 2;

        public int Bar3
        {
            get
            {
                return 3;
            }
        }
    }
}";
            testCode = testCode.AssertReplace("var temp = this.Bar1;", code);
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause(code).Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.Select(x => x.Value));
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.bar;", "1")]
        [TestCase("var temp2 = this.Bar;", "this.bar")]
        [TestCase("var temp3 = this.bar;", "1, 2")]
        [TestCase("var temp4 = this.Bar;", "this.bar")]
        [TestCase("var temp5 = this.bar;", "1, 2")]
        [TestCase("var temp6 = this.Bar;", "this.bar")]
        public void BackingFieldPrivateSetInitializedAndAssignedInCtor(string code1, string expected)
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
                var actual = string.Join(", ", pooled.Item.Select(x => x.Value));
                Assert.AreEqual(expected, actual);
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
                var actual = string.Join(", ", pooled.Item.Select(x => x.Value));
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.bar;", "1")]
        [TestCase("var temp2 = this.Bar;", "this.bar")]
        [TestCase("var temp3 = this.bar;", "1, 2")]
        [TestCase("var temp4 = this.Bar;", "this.bar, 2")]
        [TestCase("var temp5 = this.bar;", "1, 2")]
        [TestCase("var temp6 = this.Bar;", "this.bar, 2")]
        public void BackingFieldPrivateSetInitializedAndPropertyAssignedInCtor(string code, string expected)
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
                var actual = string.Join(", ", pooled.Item.Select(x => x.Value));
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.bar;", "1")]
        [TestCase("var temp2 = this.Bar;", "this.bar")]
        [TestCase("var temp3 = this.bar;", "1, 2")]
        [TestCase("var temp4 = this.Bar;", "this.bar, 2")]
        [TestCase("var temp5 = this.bar;", "1, 2, value")]
        [TestCase("var temp6 = this.Bar;", "this.bar, 2")]
        public void BackingFieldPublicSetInitializedAndPropertyAssignedInCtor(string code, string expected)
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
                var actual = string.Join(", ", pooled.Item.Select(x => x.Value));
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.bar;", "1")]
        [TestCase("var temp2 = this.Bar;", "this.bar")]
        [TestCase("var temp3 = this.bar;", "1, 2, 2, value / 2, 3")]
        [TestCase("var temp4 = this.Bar;", "this.bar, 2")]
        [TestCase("var temp5 = this.bar;", "1, 2, 2, value / 2, 3, value, value, value / 2, 3")]
        [TestCase("var temp6 = this.Bar;", "this.bar, 2")]
        public void BackingFieldPublicSetInitializedAndPropertyAssignedInCtorWeirdSetter(string code, string expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandBox
{
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
            set
            {
                if (true)
                {
                    this.bar = value;
                }
                else
                {
                    this.bar = value;
                }

                this.bar = value / 2;
                this.bar = 3;
            }
        }

        public void Meh()
        {
            var temp5 = this.bar;
            var temp6 = this.Bar;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.EqualsValueClause(code).Value;
            using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", pooled.Item.Select(x => x.Value));
                Assert.AreEqual(expected, actual);
            }
        }
    }
}