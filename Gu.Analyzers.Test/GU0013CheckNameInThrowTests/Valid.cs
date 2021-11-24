namespace Gu.Analyzers.Test.GU0013CheckNameInThrowTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Valid
    {
        private static readonly ObjectCreationAnalyzer Analyzer = new();

        [Test]
        public static void WhenPrivate()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        private readonly string text;

        private C(string text)
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
namespace N
{
    using System;

    public class C
    {
        private readonly string text;

        public C(string text = null)
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
namespace N
{
    using System;

    public class C
    {
        private readonly int i;

        public C(int i)
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
namespace N
{
    using System;

    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = text ?? throw new ArgumentNullException(nameof(text));
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
