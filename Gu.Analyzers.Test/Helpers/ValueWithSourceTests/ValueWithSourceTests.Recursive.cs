namespace Gu.Analyzers.Test.Helpers
{
    using System.Linq;
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;

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
    internal void Bar()
    {
        var value = this.Value;
    }

    public int Value => this.Value;
}";
                testCode = testCode.AssertReplace("this.Value", callCode);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause($"var value = {callCode};").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"{callCode} Calculated, {callCode} Recursion", actual);
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
    internal void Bar()
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
                testCode = testCode.AssertReplace("this.Value", callCode);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause($"var value = {callCode};").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"{callCode} Calculated, {callCode} Recursion", actual);
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
    internal void Bar()
    {
        var value = this.Value();
    }

    public int Value() => this.Value();
}";
                testCode = testCode.AssertReplace("this.Value()", callCode);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause($"var value = {callCode};").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"{callCode} Calculated, {callCode} Recursion", actual);
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
    internal void Bar()
    {
        var value = this.Value(1);
    }

    public int Value(int value) => this.Value(value);
}";
                testCode = testCode.AssertReplace("this.Value", callCode);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause($"var value = {callCode}(1);").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"{callCode}(1) Calculated, {callCode}(value) Recursion", actual);
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
    internal void Bar()
    {
        var value = this.Value();
    }

    public int Value()
    {
        return this.Value();
    }
}";
                testCode = testCode.AssertReplace("this.Value()", callCode);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause($"var value = {callCode};").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"{callCode} Calculated, {callCode} Recursion", actual);
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
    internal void Bar()
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
                var node = syntaxTree.EqualsValueClause($"var value = {callCode}(1);").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"{callCode}(1) Calculated, {callCode}(value) Recursion", actual);
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
    internal void Bar()
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
                var node = syntaxTree.EqualsValueClause($"var value = {callCode}(1);").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"{callCode}(1) Calculated, {callCode}(2) Recursion", actual);
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
    internal void Bar()
    {
        var value = await this.ValueAsync();
    }

    public async Task<int> ValueAsync()
    {
        return await this.ValueAsync();
    }
}";
                testCode = testCode.AssertReplace("this.ValueAsync()", callCode);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause($"var value = await {callCode};").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"await {callCode} Calculated, await {callCode} Recursion", actual);
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
    internal void Bar()
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
                var node = syntaxTree.EqualsValueClause($"var value = await {callCode}(1);").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"await {callCode}(1) Calculated, await {callCode}(value) Recursion", actual);
                }
            }

            [Test]
            public void AsyncMethodWithParameterStatementBodyHardcodedArg()
            {
                var testCode = @"
using System;
internal class Foo
{
    internal void Bar()
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
                var node = syntaxTree.EqualsValueClause("var value = await this.ValueAsync(1);").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"await this.ValueAsync(1) Calculated, await this.ValueAsync(2) Recursion", actual);
                }
            }

            [Test]
            public void AsyncMethodWithParameterExpressionBodyHardcodedArg()
            {
                var testCode = @"
using System;
internal class Foo
{
    internal void Bar()
    {
        var value = await this.ValueAsync(1);
    }

    public async Task<int> ValueAsync(int value) => await this.ValueAsync(2);
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause("var value = await this.ValueAsync(1);").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual($"await this.ValueAsync(1) Calculated, await this.ValueAsync(2) Recursion", actual);
                }
            }

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
                var node = syntaxTree.EqualsValueClause("var meh = Bar(1);").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Bar(1) Calculated, Bar(value, new[] { value }) Recursion", actual);
                }
            }

            [TestCase("private static")]
            [TestCase("private")]
            [TestCase("public")]
            [TestCase("public static")]
            public void PrivateStaticMethodWithOptionalParameterConditionalRecursion(string modifiers)
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
                var node = syntaxTree.EqualsValueClause("var meh = Bar(1);").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Bar(1) Calculated, Bar(value, new[] { value }) Recursion, value Argument, 1 Constant", actual);
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
                var node = syntaxTree.EqualsValueClause("var meh = Bar(1);").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Bar(1) Calculated, Baz(value) Recursion, Bar(value) Recursion", actual);
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
namespace RoslynSandBox
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
namespace RoslynSandBox
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
namespace RoslynSandBox
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