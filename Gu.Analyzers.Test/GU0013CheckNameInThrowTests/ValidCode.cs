namespace Gu.Analyzers.Test.GU0013CheckNameInThrowTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class ValidCode
    {
        private static readonly ObjectCreationAnalyzer Analyzer = new ObjectCreationAnalyzer();

        [Test]
        public static void WhenPrivate()
        {
            var code = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly string text;

        private Foo(string text)
        {
            this.text = text;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenDefaultValue()
        {
            var code = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly string text;

        public Foo(string text = null)
        {
            this.text = text;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenValueType()
        {
            var code = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly int i;

        public Foo(int i)
        {
            this.i = i;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenThrowing()
        {
            var code = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly string text;

        public Foo(string text)
        {
            this.text = text ?? throw new ArgumentNullException(nameof(text));
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
