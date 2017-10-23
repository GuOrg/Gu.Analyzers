// ReSharper disable InconsistentNaming
namespace Gu.Analyzers.Test.GU0011DontIgnoreReturnValueTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class HappyPath
    {
        internal class Ignore
        {
            [Test]
            public void StringBuilderAppendLine()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Text;

    public class Foo
    {
        public void Bar()
        {
            var sb = new StringBuilder();
            sb.AppendLine(""test"");
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void StringBuilderAppend()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Text;

    public class Foo
    {
        public void Bar()
        {
            var sb = new StringBuilder();
            sb.Append(""test"");
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void StringBuilderAppendChained()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Text;

    public class Foo
    {
        public void Bar()
        {
            var sb = new StringBuilder();
            sb.Append(""1"").Append(""2"");
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void WhenReturningThis()
            {
                var testCode = @"
namespace RoslynSandbox
{
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
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void WhenExtensionMethodReturningThis()
            {
                var barCode = @"
namespace RoslynSandbox
{
    internal static class Bar
    {
        internal static T Id<T>(this T value)
        {
            return value;
        }
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private Foo()
        {
            var meh =1;
            meh.Id();
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, barCode, testCode);
            }

            [Explicit("Don't know if we want this.")]
            [TestCase("this.ints.Add(1);")]
            [TestCase("ints.Add(1);")]
            [TestCase("this.ints.Remove(1);")]
            public void HashSet(string operation)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    public sealed class Foo
    {
        private readonly HashSet<int> ints = new HashSet<int>();

        public Foo()
        {
            this.ints.Add(1);
        }
    }
}";
                testCode = testCode.AssertReplace("this.ints.Add(1);", operation);
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [TestCase("this.ints.Add(1);")]
            [TestCase("ints.Add(1);")]
            [TestCase("this.ints.Remove(1);")]
            public void IList(string operation)
            {
                var testCode = @"
namespace RoslynSandbox
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
                AnalyzerAssert.Valid(Analyzer, testCode);
            }
        }
    }
}