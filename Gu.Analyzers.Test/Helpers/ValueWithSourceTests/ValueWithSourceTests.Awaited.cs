//namespace Gu.Analyzers.Test.Helpers
//{
//    using System.Linq;
//    using System.Threading;

//    using Microsoft.CodeAnalysis.CSharp;

//    using NUnit.Framework;

//    internal partial class ValueWithSourceTests
//    {
//        public class Awaited
//        {
//            [Test]
//            public void AsyncMethodConfigureAwaitSyntaxError()
//            {
//                var syntaxTree = CSharpSyntaxTree.ParseText(@"
//using System.Threading.Tasks;

//internal class Foo
//{
//    internal static async Task Bar()
//    {
//        var text = await CreateAsync().ConfigureAwait(false);
//    }

//    internal static async Task<string> CreateAsync()
//    {
//        await Task.Delay(0);
//        return await Task.SyntaxError(() => new string(' ', 1)).ConfigureAwait(false);
//    }
//}");
//                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
//                var semanticModel = compilation.GetSemanticModel(syntaxTree);
//                var node = syntaxTree.EqualsValueClause("var text = await CreateAsync().ConfigureAwait(false);").Value;
//                using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
//                {
//                    var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
//                    Assert.AreEqual("await CreateAsync().ConfigureAwait(false) Calculated, await Task.SyntaxError(() => new string(' ', 1)).ConfigureAwait(false) Unknown", actual);
//                }
//            }
//        }
//    }
//}