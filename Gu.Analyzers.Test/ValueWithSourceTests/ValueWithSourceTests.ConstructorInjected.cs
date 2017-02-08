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
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
                Assert.AreEqual("var value = meh;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("meh Injected", actual);
                }
            }

            [Test]
            public void SimpleValuePassedToIdentityMethod()
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
            Id(null);
            this.disposable = Id(disposable);
        }

        public IDisposable Id(IDisposable arg)
        {
            return arg;
        }

        public void Bar()
        {
            var temp = this.disposable;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<AssignmentExpressionSyntax>().Right;
                Assert.AreEqual("this.disposable = Id(disposable);", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Id(disposable) Calculated, arg Argument, disposable Injected", actual);
                }

                node = syntaxTree.Descendant<EqualsValueClauseSyntax>(0).Value;
                Assert.AreEqual("var temp = this.disposable;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.disposable Member, Id(disposable) Calculated, arg Argument, disposable Injected", actual);
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

        public Foo(IDisposable arg)
        {
            this.disposable = Bar(arg);
        }

        private static IDisposable Bar(IDisposable disposable, IEnumerable<IDisposable> disposables = null)
        {
            if (disposables == null)
            {
                return Bar(disposable, new[] { disposable });
            }

            return disposable;
        }

        public void Bar()
        {
            var temp = this.disposable;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<AssignmentExpressionSyntax>().Right;
                Assert.AreEqual("this.disposable = Bar(arg);", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("Bar(arg) Calculated, Bar(disposable, new[] { disposable }) Calculated, Bar(disposable, new[] { disposable }) Recursion, disposable Argument, disposable Argument, arg Injected, disposable Recursion, disposable Recursion", actual);
                }

                node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
                Assert.AreEqual("var temp = this.disposable;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.disposable Member, Bar(arg) Calculated, Bar(disposable, new[] { disposable }) Calculated, Bar(disposable, new[] { disposable }) Recursion, disposable Argument, disposable Argument, arg Injected, disposable Recursion, disposable Recursion", actual);
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
                Assert.AreEqual("var value = disposable.Disposable;", node.FirstAncestor<StatementSyntax>().ToString());
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
                Assert.AreEqual("var value = disposable?.Disposable;", node.FirstAncestor<StatementSyntax>().ToString());
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
        var value = this.meh?.Disposable;
    }

    protected void Bar()
    {
        var temp = this.meh?.Disposable;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
                Assert.AreEqual("var value = this.meh?.Disposable;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.meh?.Disposable External, this.meh Member, meh Injected", actual);
                }

                node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
                Assert.AreEqual("var temp = this.meh?.Disposable;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.meh?.Disposable External, this.meh Member, meh Injected", actual);
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
                Assert.AreEqual("var value = meh?.Disposable?.GetHashCode();", node.FirstAncestor<StatementSyntax>().ToString());
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

    protected void Bar()
    {
        var temp = this.meh?.Disposable?.GetHashCode();
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
                Assert.AreEqual("var value = this.meh?.Disposable?.GetHashCode();", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.meh?.Disposable?.GetHashCode() External, this.meh Member, meh Injected", actual);
                }

                node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
                Assert.AreEqual("var temp = this.meh?.Disposable?.GetHashCode();", node.FirstAncestor<StatementSyntax>().ToString());
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
                Assert.AreEqual("var value = meh?.Disposable.GetHashCode();", node.FirstAncestor<StatementSyntax>().ToString());
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

    protected void Bar()
    {
        var temp = this.meh?.Disposable.GetHashCode();
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
                Assert.AreEqual("var value = this.meh?.Disposable.GetHashCode();", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.meh?.Disposable.GetHashCode() External, this.meh Member, meh Injected", actual);
                }

                node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
                Assert.AreEqual("var temp = this.meh?.Disposable.GetHashCode();", node.FirstAncestor<StatementSyntax>().ToString());
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

    protected void Bar()
    {
        var temp = this.meh?.Disposable?.GetHashCode();
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
                Assert.AreEqual("var value = this.meh?.Disposable?.GetHashCode();", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("this.meh?.Disposable?.GetHashCode() External, this.meh Member, meh Injected", actual);
                }

                node = syntaxTree.Descendant<EqualsValueClauseSyntax>(1).Value;
                Assert.AreEqual("var temp = this.meh?.Disposable?.GetHashCode();", node.FirstAncestor<StatementSyntax>().ToString());
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
                Assert.AreEqual("var value = temp;", node.FirstAncestor<StatementSyntax>().ToString());
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
                Assert.AreEqual("var value = meh;", node.FirstAncestor<StatementSyntax>().ToString());
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
                Assert.AreEqual("var value = meh;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("meh Argument, 1 Constant", actual);
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
                Assert.AreEqual("var value = meh;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("meh Argument, text.Length External, text Injected", actual);
                }
            }

            [Test]
            public void ChainedPrivateWithOneArg()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    public Foo(string text, int meh)
        : this(meh)
    {
    }

    private Foo(int arg)
    {
        var value = arg;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
                Assert.AreEqual("var value = arg;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("arg Argument, meh Injected", actual);
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
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
                Assert.AreEqual("var value = meh;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("meh PotentiallyInjected, gg Injected", actual);
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
                Assert.AreEqual("var value = meh;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("meh PotentiallyInjected, 1 Constant, 2 Constant, text.Length External, text Injected", actual);
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
                Assert.AreEqual("var temp = value;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("value Argument, 1 Constant", actual);
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
                Assert.AreEqual("var temp = value;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("value Argument, CachedDisposable Cached", actual);
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
                Assert.AreEqual("var temp = value;", node.FirstAncestor<StatementSyntax>().ToString());
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("value Argument, ConstValue Cached", actual);
                }
            }
        }
    }
}