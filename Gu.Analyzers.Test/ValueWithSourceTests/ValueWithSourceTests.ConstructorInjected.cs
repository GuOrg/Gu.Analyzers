namespace Gu.Analyzers.Test
{
    using System.Linq;
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using NUnit.Framework;

    public partial class ValueWithSourceTests
    {
        public class ConstructorInjected
        {
            [Test]
            public void SimpleValue()
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
            public void SimpleValuePassedToStaticMethodWithOptionalParameter()
            {
                var testCode = @"
namespace RoslynSandBox
{
    using System;
    using System.Collections.Generic;

    public class Foo
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            this.disposable = Bar(disposable);
        }

        private static IDisposable Bar(IDisposable disposable, IEnumerable<IDisposable> disposables = null)
        {
            if (disposables == null)
            {
                return Bar(disposable, new[] { disposable });
            }

            return disposable;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<AssignmentExpressionSyntax>().Right;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("disposable Injected", actual);
                }
            }

            [Test]
            public void NestedValue()
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
            public void NestedElvisValue()
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
            public void MemberNestedElvisValue()
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
            public void NestedElvisValues()
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
            public void MemberNestedElvisValues()
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
            public void NestedElvisValues2()
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
            public void MemberNestedElvisValues2()
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
            public void MemberNestedElvisValues3()
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
            public void AssignedToVariable()
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
            public void PrivateNoChained()
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
            public void ChainedPrivate()
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
            public void ChainedPrivatePropertyOnInjected()
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
            public void ChainedPrivatePart()
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
            public void Chained4()
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
            public void ChainedDefaultValue()
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
            public void PrivateInjectedFactoryConstant()
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
            public void PrivateInjectedFactoryCached()
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
            public void PrivateInjectedFactoryConst()
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
        }
    }
}