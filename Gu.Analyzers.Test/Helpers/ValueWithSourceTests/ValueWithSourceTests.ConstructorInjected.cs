﻿namespace Gu.Analyzers.Test.Helpers
{
    using System.Linq;
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;

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
                var node = syntaxTree.EqualsValueClause("var value = meh;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("meh Injected", actual);
                }
            }

            [TestCase("var temp1 = this.disposable;", "this.disposable Member, Id(disposable) Calculated, arg Argument, disposable Injected")]
            [TestCase("var temp2 = this.disposable;", "this.disposable Member, Id(disposable) Calculated, arg Argument, disposable Injected")]
            public void InjectedValuePassedToIdentityMethod(string code, string expected)
            {
                var testCode = @"
namespace RoslynSandBox
{
    using System;

    public class Foo
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            Id(null);
            this.disposable = Id(disposable);
            var temp1 = this.disposable;
        }

        public IDisposable Id(IDisposable arg)
        {
            return arg;
        }

        public void Bar()
        {
            var temp2 = this.disposable;
        }
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

            [TestCase("var temp1 = this.disposable;", "this.disposable Member, Bar(disposable) Calculated, arg Argument, new Disposable() Created, disposable Injected")]
            [TestCase("var temp2 = this.disposable;", "this.disposable Member, Bar(disposable) Calculated, arg Argument, new Disposable() Created, disposable Injected")]
            public void InjectedValuePassedToMethodAssigningParameter(string code, string expected)
            {
                var testCode = @"
namespace RoslynSandBox
{
    using System;

    public class Foo
    {
        private readonly IDisposable disposable;

        public Foo(IDisposable disposable)
        {
            Id(null);
            this.disposable = Bar(disposable);
            var temp1 = this.disposable;
        }

        public IDisposable Bar(IDisposable arg)
        {
            arg = new Disposable();
            return arg;
        }

        public void Bar()
        {
            var temp2 = this.disposable;
        }
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

            [TestCase("this.disposable = Bar(arg);", "Bar(arg) Calculated, Bar(disposable, new[] { disposable }) Recursion, disposable Argument, arg Injected")]
            [TestCase("var temp = this.disposable;", "this.disposable Member, Bar(disposable, new[] { disposable }) Recursion, disposable Argument, arg Injected")]
            public void SimpleValuePassedToStaticMethodWithOptionalParameter(string code, string expected)
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
                var node = syntaxTree.AssignmentExpression(code).Right;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
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
                var node = syntaxTree.EqualsValueClause("var value = disposable.Disposable;").Value;
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
                var node = syntaxTree.EqualsValueClause("var value = disposable?.Disposable;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("disposable?.Disposable External, disposable Injected", actual);
                }
            }

            [TestCase("var temp1 = this.meh?.Disposable;", "this.meh?.Disposable External, this.meh Member, meh Injected")]
            [TestCase("var temp2 = this.meh?.Disposable;", "this.meh?.Disposable External, this.meh Member, meh Injected")]
            public void MemberNestedElvisValue(string code, string expected)
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
        var temp1 = this.meh?.Disposable;
    }

    protected void Bar()
    {
        var temp2 = this.meh?.Disposable;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
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
                var node = syntaxTree.EqualsValueClause("var value = meh?.Disposable?.GetHashCode();").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("meh?.Disposable?.GetHashCode() External, meh Injected", actual);
                }
            }

            [TestCase("var temp1 = this.meh?.Disposable?.GetHashCode();", "this.meh?.Disposable?.GetHashCode() External, this.meh Member, meh Injected")]
            [TestCase("var temp2 = this.meh?.Disposable?.GetHashCode();", "this.meh?.Disposable?.GetHashCode() External, this.meh Member, meh Injected")]
            public void MemberNestedElvisValues(string code, string expected)
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
        var temp1 = this.meh?.Disposable?.GetHashCode();
    }

    protected void Bar()
    {
        var temp2 = this.meh?.Disposable?.GetHashCode();
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
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
                var node = syntaxTree.EqualsValueClause("var value = meh?.Disposable.GetHashCode();").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("meh?.Disposable.GetHashCode() External, meh Injected", actual);
                }
            }

            [TestCase("var value = this.meh?.Disposable.GetHashCode();", "this.meh?.Disposable.GetHashCode() External, this.meh Member, meh Injected")]
            [TestCase("var temp = this.meh?.Disposable.GetHashCode();", "this.meh?.Disposable.GetHashCode() External, this.meh Member, meh Injected")]
            public void MemberNestedElvisValues2(string code, string expected)
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
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var value = this.meh?.Disposable?.GetHashCode();", "this.meh?.Disposable?.GetHashCode() External, this.meh Member, meh Injected")]
            [TestCase("var temp = this.meh?.Disposable?.GetHashCode();", "this.meh?.Disposable?.GetHashCode() External, this.meh Member, meh Injected")]
            public void MemberNestedElvisValues3(string code, string expected)
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
                var node = syntaxTree.EqualsValueClause(code).Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual(expected, actual);
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
                var node = syntaxTree.EqualsValueClause("var value = temp;").Value;
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
                var node = syntaxTree.EqualsValueClause("var value = meh;").Value;
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
                var node = syntaxTree.EqualsValueClause("var value = meh;").Value;
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
                var node = syntaxTree.EqualsValueClause("var value = meh;").Value;
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
                var node = syntaxTree.EqualsValueClause("var value = arg;").Value;
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
                var node = syntaxTree.EqualsValueClause("var value = meh;").Value;
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
                var node = syntaxTree.EqualsValueClause("var value = meh;").Value;
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
                var node = syntaxTree.EqualsValueClause("var temp = value;").Value;
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
                var node = syntaxTree.EqualsValueClause("var temp = value;").Value;
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
                var node = syntaxTree.EqualsValueClause("var temp = value;").Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("value Argument, ConstValue Cached", actual);
                }
            }
        }
    }
}