namespace Gu.Analyzers.Test.Helpers
{
    using System.Linq;
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;

    using NUnit.Framework;

    internal partial class ValueWithSourceTests
    {
        internal class Recursion
        {
            [Test]
            public void MethodOutParameterStatementBody()
            {
                var testCode = @"
internal class Foo
{
    internal void Bar()
    {
        int temp;
        this.Value(out temp);
        var meh = temp;
    }

    public void Value(out int value)
    {
        return this.Value(out value);
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var meh = temp;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"this.Value(out temp) Out, this.Value(out value) Recursion", actual);
                }
            }

            [Test]
            public void MethodOutParameterExpressiontBody()
            {
                var testCode = @"
internal class Foo
{
    internal void Bar()
    {
        int temp;
        this.Value(out temp);
        var temp1 = temp;
    }

    public void Value(out int value) => this.Value(out value);
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp1 = temp;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"this.Value(out temp) Out, this.Value(out value) Recursion", actual);
                }
            }

            [TestCase("private static")]
            [TestCase("private")]
            [TestCase("public")]
            [TestCase("public static")]
            public void PrivateStaticMethodWithOptionalParameter1(string modifiers)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class Foo
    {
        public Foo()
        {
            var meh = Bar(1);
        }

        private static int Bar(int value, IEnumerable<int> values = null)
        {
            return Bar(value, new[] { value });
        }
    }
}";
                testCode = testCode.AssertReplace("private static", modifiers);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var meh = Bar(1);").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Bar(1) Calculated, Bar(value, new[] { value }) Recursion", actual);
                }
            }

            [Test]
            public void PrivateStaticMethodWithOptionalParameter2()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class Foo
    {
        public Foo(IDisposable disposable)
        {
            var temp = Bar(disposable);
        }

        private static IDisposable Bar(IDisposable disposable, List<IDisposable> list = null)
        {
            if (list == null)
            {
                list = new List<IDisposable>();
            }

            if (list.Contains(disposable))
            {
                return new Disposable();
            }

            list.Add(disposable);
            return Bar(disposable, list);
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var temp = Bar(disposable);").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Bar(disposable) Calculated, new Disposable() Created, Bar(disposable, list) Recursion", actual);
                }
            }

            [TestCase("private static")]
            [TestCase("private")]
            [TestCase("public")]
            [TestCase("public static")]
            public void PrivateStaticMethodWithOptionalParameterConditionalRecursion(string modifiers)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class Foo
    {
        public Foo()
        {
            var meh = Bar(1);
        }

        private static int Bar(int value, IEnumerable<int> values = null)
        {
            if (values == null)
            {
                return Bar(value, new[] { value });
            }

            return value;
        }
    }
}";
                testCode = testCode.AssertReplace("private static", modifiers);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var meh = Bar(1);").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Bar(1) Calculated, Bar(value, new[] { value }) Recursion, value Recursion, 1 Constant", actual);
                }
            }

            [Test]
            public void AssigningLocalWithSelf()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class Foo
    {
        public Foo()
        {
            int meh = 1;
            meh = meh;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("int meh = 1;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("1 Constant", actual);
                }

                node = syntaxTree.EqualsValueClause("int meh = 1;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("1 Constant", actual);
                }
            }

            [TestCase("this.value = this.value; // first", "this.value Recursion, this.value Recursion")]
            [TestCase("this.value = this.value; // second", "this.value Recursion, this.value Recursion")]
            public void AssigningFieldWithSelf(string code, string expected)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class Foo
    {
        private int value;

        public Foo()
        {
            this.value = this.value; // first
        }

        public void Meh()
        {
            this.value = this.value; // second
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.AssignmentExpression(code).Right;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("this.Value = this.Value; // first", "this.Value Recursion, this.Value Recursion")]
            [TestCase("this.Value = this.Value; // second", "this.Value Recursion, this.Value Recursion")]
            public void AssigningPropertyPrivateSetWithSelf(string code, string expected)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class Foo
    {
        public Foo()
        {
            this.Value = this.Value; // first
        }

        public int Value { get; private set; }


        public void Meh()
        {
            this.Value = this.Value; // second
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.AssignmentExpression(code).Right;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [Test]
            public void AssigningPropertyPublicSetWithSelf()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class Foo
    {
        public Foo()
        {
            this.Value = this.Value; // first
        }

        public int Value { get; set; }

        public void Meh()
        {
            this.Value = this.Value; // second
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.AssignmentExpression("this.Value = this.Value; // first").Right;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.Value Recursion, this.Value Recursion", actual);
                }

                node = syntaxTree.AssignmentExpression("this.Value = this.Value; // second").Right;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.Value Recursion, this.Value PotentiallyInjected, this.Value Recursion, this.Value PotentiallyInjected", actual);
                }
            }
        }
    }
}