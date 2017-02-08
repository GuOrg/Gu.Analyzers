namespace Gu.Analyzers.Test
{
    using System.Linq;
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using NUnit.Framework;

    public partial class ValueWithSourceTests
    {
        public class Recursive
        {
            [TestCase("Value")]
            [TestCase("this.Value")]
            public void PropertyExpressionBody(string callCode)
            {
                var testCode = @"
using System;
internal class Foo
{
    internal static void Bar()
    {
        var value = this.Value;
    }

    public int Value => this.Value;
}";
                testCode = testCode.AssertReplace("this.Value;", callCode);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>()
                                     .Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"{callCode} Calculated, {callCode} Calculated, {callCode} Recursion", actual);
                }
            }

            [TestCase("Value")]
            [TestCase("this.Value")]
            public void PropertyStatementBody(string callCode)
            {
                var testCode = @"
using System;
internal class Foo
{
    internal static void Bar()
    {
        var value = this.Value;
    }

    public int Value
    {
        get
        {
            return this.Value;
        }
    }
}";
                testCode = testCode.AssertReplace("this.Value;", callCode);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>()
                                     .Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"{callCode} Calculated, {callCode} Calculated, {callCode} Recursion", actual);
                }
            }

            [TestCase("Value()")]
            [TestCase("this.Value()")]
            public void MethodExpressionBody(string callCode)
            {
                var testCode = @"
using System;
internal class Foo
{
    internal static void Bar()
    {
        var value = this.Value();
    }

    public int Value() => this.Value();
}";
                testCode = testCode.AssertReplace("this.Value();", callCode);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>()
                                     .Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"{callCode} Calculated, {callCode} Calculated, {callCode} Recursion", actual);
                }
            }

            [TestCase("Value")]
            [TestCase("this.Value")]
            public void MethodWithParameterExpressionBody(string callCode)
            {
                var testCode = @"
using System;
internal class Foo
{
    internal static void Bar()
    {
        var value = this.Value(1);
    }

    public int Value(int value) => this.Value(value);
}";
                testCode = testCode.AssertReplace("this.Value", callCode);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"{callCode}(1) Calculated, {callCode}(value) Calculated, {callCode}(value) Recursion", actual);
                }
            }

            [TestCase("Value()")]
            [TestCase("this.Value()")]
            public void MethodStatementBody(string callCode)
            {
                var testCode = @"
using System;
internal class Foo
{
    internal static void Bar()
    {
        var value = this.Value();
    }

    public int Value()
    {
        return this.Value();
    }
}";
                testCode = testCode.AssertReplace("this.Value();", callCode);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>()
                                     .Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"{callCode} Calculated, {callCode} Calculated, {callCode} Recursion", actual);
                }
            }

            [TestCase("Value")]
            [TestCase("this.Value")]
            public void MethodWithParameterStatementBody(string callCode)
            {
                var testCode = @"
using System;
internal class Foo
{
    internal static void Bar()
    {
        var value = this.Value(1);
    }

    public int Value(int value)
    {
        return this.Value(value);
    }
}";
                testCode = testCode.AssertReplace("this.Value", callCode);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>()
                                     .Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"{callCode}(1) Calculated, {callCode}(value) Calculated, {callCode}(value) Recursion", actual);
                }
            }

            [TestCase("Value")]
            [TestCase("this.Value")]
            public void MethodWithParameterStatementBodyHardcodedArg(string callCode)
            {
                var testCode = @"
using System;
internal class Foo
{
    internal static void Bar()
    {
        var value = this.Value(1);
    }

    public int Value(int value)
    {
        return this.Value(2);
    }
}";
                testCode = testCode.AssertReplace("this.Value", callCode);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>()
                                     .Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"{callCode}(1) Calculated, {callCode}(2) Calculated, {callCode}(2) Recursion", actual);
                }
            }

            [TestCase("ValueAsync()")]
            [TestCase("this.ValueAsync()")]
            public void AsyncMethodStatementBody(string callCode)
            {
                var testCode = @"
using System;
internal class Foo
{
    internal static void Bar()
    {
        var value = await this.ValueAsync();
    }

    public async Task<int> ValueAsync()
    {
        return await this.ValueAsync();
    }
}";
                testCode = testCode.AssertReplace("this.ValueAsync();", callCode);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>()
                                     .Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(
                        $"await {callCode} Calculated, await {callCode} Calculated, await {callCode} Recursion",
                        actual);
                }
            }

            [TestCase("ValueAsync")]
            [TestCase("this.ValueAsync")]
            public void AsyncMethodWithParameterStatementBody(string callCode)
            {
                var testCode = @"
using System;
internal class Foo
{
    internal static void Bar()
    {
        var value = await this.ValueAsync(1);
    }

    public async Task<int> ValueAsync(int value)
    {
        return await this.ValueAsync(value);
    }
}";
                testCode = testCode.AssertReplace("this.ValueAsync", callCode);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>()
                                     .Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"await {callCode}(1) Calculated, await {callCode}(value) Calculated, await {callCode}(value) Recursion", actual);
                }
            }

            [Test]
            public void AsyncMethodWithParameterStatementBodyHardcodedArg()
            {
                var testCode = @"
using System;
internal class Foo
{
    internal static void Bar()
    {
        var value = await this.ValueAsync(1);
    }

    public async Task<int> ValueAsync(int value)
    {
        return await this.ValueAsync(2);
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>()
                                     .Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"await this.ValueAsync(1) Calculated, await this.ValueAsync(2) Calculated, await this.ValueAsync(2) Recursion", actual);
                }
            }

            [Test]
            public void MethodOutParameterStatementBody()
            {
                var testCode = @"
internal class Foo
{
    internal static void Bar()
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
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>()
                                     .Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"this.Value(out temp) Out, this.Value(out value) Out, this.Value(out value) Recursion", actual);
                }
            }

            [Test]
            public void MethodOutParameterExpressiontBody()
            {
                var testCode = @"
internal class Foo
{
    internal static void Bar()
    {
        int temp;
        this.Value(out temp);
        var meh = temp;
    }

    public void Value(out int value) => this.Value(out value);
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"this.Value(out temp) Out, this.Value(out value) Out, this.Value(out value) Recursion", actual);
                }
            }

            [TestCase("private static")]
            [TestCase("private")]
            [TestCase("public")]
            [TestCase("public static")]
            public void PrivateStaticMethodWithOptionalParameter1(string modifiers)
            {
                var testCode = @"
namespace RoslynSandBox
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
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>()
                                     .Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Bar(1) Calculated, Bar(value, new[] { value }) Calculated, Bar(value, new[] { value }) Recursion", actual);
                }
            }

            [TestCase("private static")]
            [TestCase("private")]
            [TestCase("public")]
            [TestCase("public static")]
            public void PrivateStaticMethodWithOptionalParameter2(string modifiers)
            {
                var testCode = @"
namespace RoslynSandBox
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
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>()
                                     .Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Bar(1) Calculated, Bar(value, new[] { value }) Calculated, Bar(value, new[] { value }) Recursion, value Argument, value Recursion, 1 Constant", actual);
                }
            }

            [Test]
            public void PrivateStaticMethodLoop()
            {
                var testCode = @"
namespace RoslynSandBox
{
    using System;
    using System.Collections.Generic;

    public class Foo
    {
        public Foo()
        {
            var meh = Bar(1);
        }

        private static int Bar(int value)
        {
            return Baz(value);
        }

        private static int Baz(int value)
        {
            return Bar(value);
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Bar(1) Calculated, Baz(value) Calculated, Bar(value) Calculated, Baz(value) Recursion", actual);
                }
            }

            [Test]
            public void AssigningLocalWithSelf()
            {
                var testCode = @"
namespace RoslynSandBox
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
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("1 Constant", actual);
                }

                node = syntaxTree.Descendant<AssignmentExpressionSyntax>().Right;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("1 Constant", actual);
                }
            }

            [Test]
            public void AssigningFieldWithSelf()
            {
                var testCode = @"
namespace RoslynSandBox
{
    using System;
    using System.Collections.Generic;

    public class Foo
    {
        private int value;

        public Foo()
        {
            value = value;
        }

        public void Meh()
        {
            value = value;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<AssignmentExpressionSyntax>(0).Right;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("value Member, value Recursion, value Member, value Recursion", actual);
                }

                node = syntaxTree.Descendant<AssignmentExpressionSyntax>(1).Right;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("value Member, value Recursion, value Member, value Recursion", actual);
                }
            }

            [Test]
            public void AssigningPropertyPrivateSetWithSelf()
            {
                var testCode = @"
namespace RoslynSandBox
{
    using System;
    using System.Collections.Generic;

    public class Foo
    {
        public Foo()
        {
            value = value;
        }

        public void Meh()
        {
            value = value;
        }

        public int value { get; private set; }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<AssignmentExpressionSyntax>(0).Right;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("value Member, value Recursion, value Member, value Recursion", actual);
                }

                node = syntaxTree.Descendant<AssignmentExpressionSyntax>(1).Right;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("value Member, value Recursion, value Member, value Recursion", actual);
                }
            }

            [Test]
            public void AssigningPropertyPublicSetWithSelf()
            {
                var testCode = @"
namespace RoslynSandBox
{
    using System;
    using System.Collections.Generic;

    public class Foo
    {
        public Foo()
        {
            this.Value = this.Value;
        }

        public int Value { get; set; }

        public void Meh()
        {
            this.Value = this.Value;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<AssignmentExpressionSyntax>(0).Right;
                Assert.AreEqual("var temp1 = this.Value;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.Value Member", actual);
                }

                node = syntaxTree.Descendant<AssignmentExpressionSyntax>(1).Right;
                Assert.AreEqual("var temp1 = this.Value;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.Value Member, this.Value PotentiallyInjected", actual);
                }
            }
        }
    }
}