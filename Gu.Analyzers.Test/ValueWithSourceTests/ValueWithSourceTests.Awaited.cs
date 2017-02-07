namespace Gu.Analyzers.Test
{
    using System.Linq;
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using NUnit.Framework;

    public partial class ValueWithSourceTests
    {
        public class Awaited
        {
            [Test]
            public void AsyncMethod()
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
            public void AsyncMethodThatReturnsTaskRunConfigureAwait()
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
                    Assert.AreEqual("await CreateAsync().ConfigureAwait(false) Calculated, await Task.Run(() => new string(' ', 1)).ConfigureAwait(false) External, new string(' ', 1) Created", actual);
                }
            }

            [Test]
            public void AsyncMethodConfigureAwaitSyntaxError()
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
            public void TaskFromResult()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Threading.Tasks;

internal class Foo
{
    internal static async Task Bar()
    {
        var text = await Task.FromResult(new string(' ', 1));
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("await Task.FromResult(new string(' ', 1)) External, new string(' ', 1) Created", actual);
                }
            }

            [Test]
            public void TaskRun()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Threading.Tasks;

internal class Foo
{
    internal static async Task Bar()
    {
        var text = await Task.Run(() => new string(' ', 1));
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("await Task.Run(() => new string(' ', 1)) External, new string(' ', 1) Created", actual);
                }
            }

            [Test]
            public void TernaryTaskRun()
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Threading.Tasks;

internal class Foo
{
    internal static async Task Bar(bool condition)
    {
        var text = condition 
                    ? await Task.Run(() => new string('0', 1))
                    : await Task.Run(() => new string('1', 1));
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("await Task.Run(() => new string('0', 1)) External, new string('0', 1) Created, await Task.Run(() => new string('1', 1)) External, new string('1', 1) Created", actual);
                }
            }

            [Test]
            public void MethodReturningTaskFromResult()
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
                var node = syntaxTree.Descendant<EqualsValueClauseSyntax>().Value;
                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                    Assert.AreEqual("await CreateAsync() Calculated, new string(' ', 1) Created", actual);
                }
            }

            [Test]
            public void MethodReturningTaskRun()
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
            public void MethodReturningTasksFromResult()
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
            public void MethodReturningTasksRun()
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
            public void MethodReturningTasksRunConfigureAwait()
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
            public void TasksRunAndFromResult()
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
}