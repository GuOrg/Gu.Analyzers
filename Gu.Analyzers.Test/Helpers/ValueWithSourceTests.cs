namespace Gu.Analyzers.Test
{
    using System.Linq;
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using NUnit.Framework;

    public class ValueWithSourceTests
    {
        [TestCase("1", "1 Constant")]
        [TestCase(@"""1""", @"""1"" Constant")]
        [TestCase("new string('1', 1)", "new string('1', 1) Created")]
        [TestCase("new int[2]", "new int[2] Created")]
        [TestCase("new int[] { 1 , 2 , 3 }", "new int[] { 1 , 2 , 3 } Created")]
        [TestCase("new []{ 1 , 2 , 3 }", "new []{ 1 , 2 , 3 } Created")]
        [TestCase("{ 1 , 2 , 3 }", "{ 1 , 2 , 3 } Created")]
        public void SimpleAssign(string code, string expected)
        {
            var testCode = @"
internal class Foo
{
    internal static void Bar()
    {
        var text = 1;
    }
}";
            testCode = testCode.AssertReplace("1", code);
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public void StaticMethodReturningNew()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal static void Bar()
    {
        var text = Create();
    }

    internal static string Create()
    {
        return new string(' ', 1);
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.GetRoot()
                                 .DescendantNodes()
                                 .OfType<EqualsValueClauseSyntax>()
                                 .First()
                                 .Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("new string(' ', 1) Created", actual);
            }
        }

        [Test]
        public void StaticMethodReturningNewInIfElse()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
    internal class Foo
    {
        internal static void Bar()
        {
            var text = Create(true);
        }

        internal static string Create(bool value)
        {
            if (value)
            {
                return new string('1', 1);
            }
            else
            {
                return new string('0', 1);
            }
        }
    }");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.GetRoot()
                                 .DescendantNodes()
                                 .OfType<EqualsValueClauseSyntax>()
                                 .First()
                                 .Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("new string('1', 1) Created, new string('0', 1) Created", actual);
            }
        }

        [Test]
        public void StaticMethodReturningNewExpressionBody()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
internal class Foo
{
    internal static async Task Bar()
    {
        var stream = Create();
    }

    internal static async IDisposable Create() => new Disposable();
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.GetRoot()
                                 .DescendantNodes()
                                 .OfType<EqualsValueClauseSyntax>()
                                 .First()
                                 .Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("new Disposable() Created", actual);
            }
        }

        [Test]
        public void StaticMethodReturningFileOpenRead()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
using System.IO;

public static class Foo
{
    public static long Bar()
    {
        var value = GetStream();
    }

    public static Stream GetStream()
    {
        return File.OpenRead(""A"");
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.GetRoot()
                                 .DescendantNodes()
                                 .OfType<EqualsValueClauseSyntax>()
                                 .First()
                                 .Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual(@"File.OpenRead(""A"") External", actual);
            }
        }

        [Test]
        public void AssigningWithStaticField()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
internal class Foo
{
    private static readonly int Cache = 1;
    internal static void Bar()
    {
        var value = Cache;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("Cache Cached", actual);
            }
        }

        [Test]
        public void AssigningWithStaticFieldIndexer()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
internal class Foo
{
    private static readonly int[] Cache = { 1, 2, 3 };
    internal static void Bar()
    {
        var value = Cache[1];
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("Cache[1] Cached", actual);
            }
        }

        [Test]
        public void AssigningWithStaticFieldConcurrentDictionaryGetOrAdd()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Collections.Concurrent;
using System.IO;

public static class Foo
{
    private static readonly ConcurrentDictionary<int, Stream> StreamCache = new ConcurrentDictionary<int, Stream>();

    public static void Bar()
    {
        var stream = StreamCache.GetOrAdd(1, _ => File.OpenRead(""A""));
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual(@"StreamCache.GetOrAdd(1, _ => File.OpenRead(""A"")) External, StreamCache Cached", actual);
            }
        }

        [Test]
        public void AssigningWithStaticFieldConcurrentDictionaryGetOrAddElvis()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Collections.Concurrent;
using System.IO;

public static class Foo
{
    private static readonly ConcurrentDictionary<int, Stream> StreamCache = new ConcurrentDictionary<int, Stream>();

    public static void Bar()
    {
        var stream = StreamCache?.GetOrAdd(1, _ => File.OpenRead(""A""));
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual(@"StreamCache?.GetOrAdd(1, _ => File.OpenRead(""A"")) External, StreamCache Cached", actual);
            }
        }

        [Test]
        public void AssigningWithFieldConcurrentDictionaryGetOrAdd()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Collections.Concurrent;
using System.IO;

public class Foo
{
    private readonly ConcurrentDictionary<int, Stream> Cache = new ConcurrentDictionary<int, Stream>();

    public void Bar()
    {
        var stream = Cache.GetOrAdd(1, _ => File.OpenRead(""A""));
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual(@"Cache.GetOrAdd(1, _ => File.OpenRead(""A"")) External, Cache Member, new ConcurrentDictionary<int, Stream>() Created", actual);
            }
        }

        [Test]
        public void AssigningWithFieldConcurrentDictionaryGetOrAddElvis()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Collections.Concurrent;
using System.IO;

public class Foo
{
    private readonly ConcurrentDictionary<int, Stream> Cache = new ConcurrentDictionary<int, Stream>();

    public void Bar()
    {
        var stream = Cache?.GetOrAdd(1, _ => File.OpenRead(""A""));
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual(@"Cache?.GetOrAdd(1, _ => File.OpenRead(""A"")) External, Cache Member, new ConcurrentDictionary<int, Stream>() Created", actual);
            }
        }

        [Test]
        public void AssigningWithFieldConcurrentDictionaryTryGetValue()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Collections.Concurrent;
using System.IO;

public class Foo
{
    private readonly ConcurrentDictionary<int, Stream> Cache = new ConcurrentDictionary<int, Stream>();

    public void Bar()
    {
        Stream stream;
        Cache.TryGetValue(1, out stream);
        var temp = stream;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual(@"Cache.TryGetValue(1, out stream) Out, Cache Member, new ConcurrentDictionary<int, Stream>() Created", actual);
            }
        }

        [Test]
        public void AssigningWithFieldConcurrentDictionarySyntaxError()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Collections.Concurrent;
using System.IO;

public class Foo
{
    private readonly ConcurrentDictionary<int, Stream> Cache = new ConcurrentDictionary<int, Stream>();

    public void Bar()
    {
        Steram stream;
        Cache.SyntaxError(1, out stream);
        var temp = stream;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual(@"Cache.SyntaxError(1, out stream) Unknown", actual);
            }
        }

        [Test]
        public void PropertyGetSet()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal static void Bar()
    {
        var value = Value;
    }

    public int Value { get; set; }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("Value");
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("Value Member, Value PotentiallyInjected", actual);
            }
        }

        [Test]
        public void PropertyGetSetInitialized()
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
        public void PropertyGetPublicSetWithBackingFieldAssignedWIthInjectedAndInializer()
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
                Assert.AreEqual("Value Calculated, this.value Member, 1 Constant, ctorValue Injected, value Injected", actual);
            }
        }

        [Test]
        public void PropertyGetPrivateSetWithBackingFieldAssignedInCtorAndInializer1()
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
            var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("stream");
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual(@"stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External", actual);
            }
        }

        [Test]
        public void PropertyGetPrivateSetWithBackingFieldAssignedInCtorAndInializer2()
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
        private set { this.stream = value; }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("stream");
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual(@"stream Member, File.OpenRead(""A"") External, File.OpenRead(""B"") External", actual);
            }
        }

        [Test]
        public void PropertyGetOnlyAssignedInCtorAndInializer()
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
        public void PropertyGetReturningPrivateReadonlyFieldExpressionBody()
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
        public void PropertyGetReturningPublicFieldExpressionBody()
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
        public void PropertyGetReturningFieldStatementBody()
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
        public void PropertyCalculatedStatementBody()
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
        public void PropertyCalculatedExpressionBody()
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
        public void GetOnlyPropertyStatementBodyReturningField()
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

        [TestCase("Value")]
        [TestCase("this.Value")]
        public void RecursivePropertyExpressionBody(string callCode)
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
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual($"{callCode} Calculated, {callCode} Calculated, {callCode} Recursion", actual);
            }
        }

        [TestCase("Value")]
        [TestCase("this.Value")]
        public void RecursivePropertyStatementBody(string callCode)
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
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual($"{callCode} Calculated, {callCode} Calculated, {callCode} Recursion", actual);
            }
        }

        [TestCase("Value()")]
        [TestCase("this.Value()")]
        public void RecursiveMethodExpressionBody(string callCode)
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
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual($"{callCode} Recursion", actual);
            }
        }

        [TestCase("Value()")]
        [TestCase("this.Value()")]
        public void RecursiveMethodStatementBody(string callCode)
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
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual($"{callCode} Recursion", actual);
            }
        }

        [Test]
        public void PropertyGetterReturningFieldExpressionBody()
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

        [Test]
        public void CtorInjectedSimpleValue()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo(int meh)
    {
        var value = meh;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("meh");
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("meh Injected", actual);
            }
        }

        [Test]
        public void CtorInjectedNestedValue()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
using System.IO;
using System.Reactive.Disposables;

internal abstract class Foo
{
    protected Foo(SingleAssignmentDisposable disposable)
    {
        var value = disposable.Disposable;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("disposable.Disposable External, disposable Injected", actual);
            }
        }

        [Test]
        public void CtorInjectedNestedElvisValue()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
using System.IO;
using System.Reactive.Disposables;

internal abstract class Foo
{
    protected Foo(SingleAssignmentDisposable disposable)
    {
        var value = disposable?.Disposable;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("disposable?.Disposable External, disposable Injected", actual);
            }
        }

        [Test]
        public void CtorInjectedMemberNestedElvisValue()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
using System.IO;
using System.Reactive.Disposables;

internal abstract class Foo
{
    private readonly SingleAssignmentDisposable meh;

    protected Foo(SingleAssignmentDisposable meh)
    {
        this.meh = meh;
        var value = meh?.Disposable;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("meh?.Disposable External, meh Injected", actual);
            }
        }

        [Test]
        public void CtorInjectedNestedElvisValues()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
using System.IO;
using System.Reactive.Disposables;

internal abstract class Foo
{
    protected Foo(SingleAssignmentDisposable meh)
    {
        var value = meh?.Disposable?.GetHashCode();
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("meh?.Disposable?.GetHashCode() External, meh Injected", actual);
            }
        }

        [Test]
        public void CtorInjectedMemberNestedElvisValues()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
using System.IO;
using System.Reactive.Disposables;

internal abstract class Foo
{
    private readonly SingleAssignmentDisposable meh;

    protected Foo(SingleAssignmentDisposable meh)
    {
        this.meh = meh;
        var value = this.meh?.Disposable?.GetHashCode();
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("this.meh?.Disposable?.GetHashCode() External, this.meh Member, meh Injected", actual);
            }
        }

        [Test]
        public void CtorInjectedNestedElvisValues2()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
using System.IO;
using System.Reactive.Disposables;

internal abstract class Foo
{
    protected Foo(SingleAssignmentDisposable meh)
    {
        var value = meh?.Disposable.GetHashCode();
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("meh?.Disposable.GetHashCode() External, meh Injected", actual);
            }
        }

        [Test]
        public void CtorInjectedMemberNestedElvisValues2()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
using System.IO;
using System.Reactive.Disposables;

internal abstract class Foo
{
    private readonly SingleAssignmentDisposable meh;

    protected Foo(SingleAssignmentDisposable meh)
    {
        this.meh = meh;
        var value = this.meh?.Disposable.GetHashCode();
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("this.meh?.Disposable.GetHashCode() External, this.meh Member, meh Injected", actual);
            }
        }

        [Test]
        public void CtorInjectedMemberNestedElvisValues3()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
using System.IO;
using System.Reactive.Disposables;

internal abstract class Foo
{
    private readonly SingleAssignmentDisposable meh;

    protected Foo(SingleAssignmentDisposable meh)
    {
        this.meh = meh;
        var value = this.meh?.Disposable?.GetHashCode();
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("this.meh?.Disposable?.GetHashCode() External, this.meh Member, meh Injected", actual);
            }
        }

        [Test]
        public void CtorInjectedAssignedToVariable()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo(int meh)
    {
        var temp = meh;
        var value = temp;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("temp");
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("meh Injected", actual);
            }
        }

        [Test]
        public void CtorPrivateNoChained()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    private Foo(int meh)
    {
        var value = meh;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("meh");
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));

                // Assuming Injected here since the only way to create an instance will be reflection with injection.
                Assert.AreEqual("meh Injected", actual);
            }
        }

        [Test]
        public void CtorInjectedChainedPrivate()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    public Foo()
        : this(1)
    {
    }

    private Foo(int meh)
    {
        var value = meh;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("meh");
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("1 Constant", actual);
            }
        }

        [Test]
        public void CtorInjectedChainedPrivatePropertyOnInjected()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    public Foo(string text)
        : this(text.Length)
    {
    }

    private Foo(int meh)
    {
        var value = meh;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("meh");
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("text.Length External, text Injected", actual);
            }
        }

        [Test]
        public void CtorInjectedChainedPrivatePart()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    public Foo(string text, int meh)
        : this(meh)
    {
    }

    private Foo(int meh)
    {
        var value = meh;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("meh");
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("meh Injected", actual);
            }
        }

        [Test]
        public void CtorInjectedChained4()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    public Foo(int gg, string foo)
        : this(gg)
    {
    }

    public Foo(int meh)
    {
        var value = meh;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("meh");
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("meh Injected, gg Injected", actual);
            }
        }

        [Test]
        public void CtorInjectedChainedDefaultValue()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandBox
{
    internal class Foo
    {
        public Foo()
            : this(1)
        {
        }

        public Foo(double gg)
            : this(1, 2)
        {
        }

        public Foo(string text)
            : this(1, text.Length)
        {
        }

        public Foo(int _, int meh = 1)
        {
            var value = meh;
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("meh");
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("meh Injected, 1 Constant, 2 Constant, text.Length External, text Injected", actual);
            }
        }

        [Test]
        public void CtorPrivateInjectedFactoryConstant()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
public sealed class Foo
{
    private Foo(int value)
    {
        var temp = value;
    }

    public static Foo Create()
    {
        return new Foo(1);
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("value");
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("1 Constant", actual);
            }
        }

        [Test]
        public void CtorPrivateInjectedFactoryCached()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;

public sealed class Foo
{
    private static readonly IDisposable CachedDisposable = new Disposable();

    private Foo(IDisposable value)
    {
        var temp = value;
    }

    public static Foo Create() => new Foo(CachedDisposable);
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("value");
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("CachedDisposable Cached", actual);
            }
        }

        [Test]
        public void CtorPrivateInjectedFactoryConst()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;

public sealed class Foo
{
    private const int ConstValue = 1;

    private Foo(int value)
    {
        var temp = value;
    }

    public static Foo Create() => new Foo(ConstValue);
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("value");
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("ConstValue Cached", actual);
            }
        }

        [Test]
        public void MethodInjected()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal static void Bar(int meh)
    {
        var value = meh;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("meh");
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("meh Injected", actual);
            }
        }

        [Test]
        public void MethodInjectedWithOptional()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal static void Meh()
    {
        Bar(1);
        Bar(2, ""abc"");
    }

    internal static void Bar(int meh, string text = null)
    {
        var value = meh;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("meh Injected, 1 Constant, 2 Constant", actual);
            }
        }

        [Test]
        public void MethodInjectedWithOptionalAssigningOptional()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal static void Meh()
    {
        Bar(1);
        Bar(2, ""abc"");
    }

    internal static void Bar(int meh, string text = null)
    {
        var value = text;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual(@"text Injected, null Constant, ""abc"" Constant", actual);
            }
        }

        [TestCase("private void Assign")]
        [TestCase("public void Assign")]
        [TestCase("public static void Assign")]
        public void FieldPrivateAssignedWithOutParameter(string code)
        {
            var testCode = @"
internal class Foo
{
    private int field;

    internal void Bar()
    {
        var value = this.field;
        this.Assign(out this.field);
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
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("this.field Member, this.Assign(out this.field) Out, 1 Constant", actual);
            }
        }

        [TestCase("private void Assign")]
        [TestCase("public void Assign")]
        [TestCase("public static void Assign")]
        public void FieldAssignedWithRefParameter(string code)
        {
            var testCode = @"
internal class Foo
{
    private int field;

    internal void Bar()
    {
        var value = this.field;
        Assign(ref this.field);
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
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("this.field Member, Assign(ref this.field) Ref, 1 Constant", actual);
            }
        }

        [Test]
        public void FieldPublicInitialized()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    public int field = 1;

    internal void Bar()
    {
        var value = this.field;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("field");
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("field Member, field PotentiallyInjected, 1 Constant", actual);
            }
        }

        [TestCase("{ 1, 2, 3 }")]
        [TestCase("new [] { 1, 2, 3 }")]
        [TestCase("new int[] { 1, 2, 3 }")]
        public void FieldPrivateInitializedIndexer(string collection)
        {
            var testCode = @"
internal class Foo
{
    public int[] field = { 1, 2, 3 };

    internal void Bar()
    {
        var value = this.field[1];
    }
}";
            testCode = testCode.AssertReplace("{ 1, 2, 3 }", collection);
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("field");
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual($"field Member, field PotentiallyInjected, {collection} Created", actual);
            }
        }

        [TestCase("public readonly")]
        [TestCase("private readonly")]
        [TestCase("private")]
        public void FieldInitialized(string modifiers)
        {
            var testCode = @"
internal class Foo
{
    public readonly int field = 1;

    internal void Bar()
    {
        var value = this.field;
    }
}";
            testCode = testCode.AssertReplace("public readonly", modifiers);
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("field");
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("field Member, 1 Constant", actual);
            }
        }

        [Test]
        public void VariableAssignedWithOutParameter()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal void Bar()
    {
        int value;
        this.Assign(out value);
        var temp = value;
        var meh = temp;
    }

    private void Assign(out int value)
    {
        value = 1;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("this.Assign(out value) Out, 1 Constant", actual);
            }
        }

        [Test]
        public void VariableAssignedWithRefParameter()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    private int field;

    internal void Bar()
    {
        int value;
        this.Assign(ref value);
        var temp = value;
        var meh = temp;
    }

    private void Assign(ref int value)
    {
        value = 1;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.FirstDescendant<IdentifierNameSyntax>("temp");
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("this.Assign(ref value) Ref, 1 Constant", actual);
            }
        }

        [Test]
        public void AwaitedAsyncMethod()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Threading.Tasks;

internal class Foo
{
    internal static async Task Bar()
    {
        var text = await CreateAsync();
    }

    internal static async Task<string> CreateAsync()
    {
        await Task.Delay(0);
        return new string(' ', 1);
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("await CreateAsync() Calculated, new string(' ', 1) Created", actual);
            }
        }

        [Test]
        public void AwaitedAsyncMethodThatDoesTaskRunConfigureAwait()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Threading.Tasks;

internal class Foo
{
    internal static async Task Bar()
    {
        var text = await CreateAsync().ConfigureAwait(false);
    }

    internal static async Task<string> CreateAsync()
    {
        await Task.Delay(0);
        return await Task.Run(() => new string(' ', 1)).ConfigureAwait(false);
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("await CreateAsync().ConfigureAwait(false) Calculated, await Task.Run(() => new string(' ', 1)).ConfigureAwait(false) Calculated, new string(' ', 1) Created", actual);
            }
        }

        [Test]
        public void AwaitedAsyncMethodConfigureAwaitSyntaxError()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Threading.Tasks;

internal class Foo
{
    internal static async Task Bar()
    {
        var text = await CreateAsync().ConfigureAwait(false);
    }

    internal static async Task<string> CreateAsync()
    {
        await Task.Delay(0);
        return await Task.SyntaxError(() => new string(' ', 1)).ConfigureAwait(false);
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("await CreateAsync().ConfigureAwait(false) Calculated, await Task.SyntaxError(() => new string(' ', 1)).ConfigureAwait(false) Unknown", actual);
            }
        }

        [Test]
        public void AwaitedTaskFromResult()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Threading.Tasks;

internal class Foo
{
    internal static async Task Bar()
    {
        var text = await CreateAsync();
    }

    internal static Task<string> CreateAsync()
    {
        return Task.FromResult(new string(' ', 1));
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.GetRoot()
                                 .DescendantNodes()
                                 .OfType<EqualsValueClauseSyntax>()
                                 .First()
                                 .Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("await CreateAsync() Calculated, new string(' ', 1) Created", actual);
            }
        }

        [Test]
        public void AwaitedTaskRun()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Threading.Tasks;

internal class Foo
{
    internal static async Task Bar()
    {
        var text = await CreateAsync();
    }

    internal static Task<string> CreateAsync()
    {
        return Task.Run(() => new string(' ', 1));
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("await CreateAsync() Calculated, new string(' ', 1) Created", actual);
            }
        }

        [Test]
        public void AwaitedTasksFromResult()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Threading.Tasks;

internal class Foo
{
    internal static async Task Bar()
    {
        var text = await CreateAsync(true);
    }

    internal static Task<string> CreateAsync(bool value)
    {
        if (value)
        {
            return Task.FromResult(new string('1', 1));
        }

        return Task.FromResult(new string('0', 1));
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("await CreateAsync(true) Calculated, new string('1', 1) Created, new string('0', 1) Created", actual);
            }
        }

        [Test]
        public void AwaitedTasksRun()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Threading.Tasks;

internal class Foo
{
    internal static async Task Bar()
    {
        var text = await CreateAsync(true);
    }

    internal static Task<string> CreateAsync(bool value)
    {
        if (value)
        {
            return Task.Run(() => new string('1', 1));
        }

        return Task.Run(() => new string('0', 1));
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("await CreateAsync(true) Calculated, new string('1', 1) Created, new string('0', 1) Created", actual);
            }
        }

        [Test]
        public void AwaitedTasksRunConfigureAwait()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Threading.Tasks;

internal class Foo
{
    internal static async Task Bar()
    {
        var text = await CreateAsync(true).ConfigureAwait(false);
    }

    internal static Task<string> CreateAsync(bool value)
    {
        if (value)
        {
            return Task.Run(() => new string('1', 1));
        }

        return Task.Run(() => new string('0', 1));
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("await CreateAsync(true).ConfigureAwait(false) Calculated, new string('1', 1) Created, new string('0', 1) Created", actual);
            }
        }

        [Test]
        public void AwaitedTasksRunAndFromResult()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Threading.Tasks;

internal class Foo
{
    internal static async Task Bar()
    {
        var text = await CreateAsync(true);
    }

    internal static Task<string> CreateAsync(bool value)
    {
        if (value)
        {
            return Task.Run(() => new string('1', 1));
        }

        return Task.FromResult(new string('0', 1));
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("await CreateAsync(true) Calculated, new string('1', 1) Created, new string('0', 1) Created", actual);
            }
        }
    }
}
