// ReSharper disable InconsistentNaming
namespace Gu.Analyzers.Test.GU0011DontIgnoreReturnValueTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class HappyPath
    {
        internal class Ignore : NestedHappyPathVerifier<HappyPath>
        {
            [Test]
            public async Task StringBuilderAppendLine()
            {
                var testCode = @"
using System.Text;
public class Foo
{
    public void Bar()
    {
        var sb = new StringBuilder();
        sb.AppendLine(""test"");
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task StringBuilderAppend()
            {
                var testCode = @"
using System.Text;
public class Foo
{
    public void Bar()
    {
        var sb = new StringBuilder();
        sb.Append(""test"");
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task StringBuilderAppendChained()
            {
                var testCode = @"
using System.Text;
public class Foo
{
    public void Bar()
    {
        var sb = new StringBuilder();
        sb.Append(""1"").Append(""2"");
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task WhenReturningThis()
            {
                var testCode = @"
public class Foo
{
    public Foo Bar()
    {
        return this;
    }

    public void Meh()
    {
        Bar();
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task WhenExtensionMethodReturningThis()
            {
                var barCode = @"
internal static class Bar
{
    internal static T Id<T>(this T value)
    {
        return value;
    }
}";
                var testCode = @"
public class Foo
{
    private Foo()
    {
        var meh =1;
        meh.Id();
    }
}";
                await this.VerifyHappyPathAsync(barCode, testCode)
                          .ConfigureAwait(false);
            }

////            [Explicit("Don't know if we want this.")]
////            [TestCase("this.ints.Add(1);")]
////            [TestCase("ints.Add(1);")]
////            [TestCase("this.ints.Remove(1);")]
////            public async Task HashSet(string operation)
////            {
////                var testCode = @"
////namespace RoslynSandBox
////{
////    using System.Collections.Generic;

////    public sealed class Foo
////    {
////        private readonly HashSet<int> ints = new HashSet<int>();

////        public Foo()
////        {
////            this.ints.Add(1);
////        }
////    }
////}";
////                testCode = testCode.AssertReplace("this.ints.Add(1);", operation);
////                await this.VerifyHappyPathAsync(testCode)
////                          .ConfigureAwait(false);
////            }

            [TestCase("this.ints.Add(1);")]
            [TestCase("ints.Add(1);")]
            [TestCase("this.ints.Remove(1);")]
            public async Task IList(string operation)
            {
                var testCode = @"
namespace RoslynSandBox
{
    using System.Collections;
    using System.Collections.Generic;

    public sealed class Foo
    {
        private readonly IList ints = new List<int>();

        public Foo()
        {
            this.ints.Add(1);
        }
    }
}";
                testCode = testCode.AssertReplace("this.ints.Add(1);", operation);
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }
        }
    }
}