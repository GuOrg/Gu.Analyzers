namespace Gu.Analyzers.Test.GU0005ExceptionArgumentsPositionsTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class ValidCode
    {
        private static readonly ObjectCreationAnalyzer Analyzer = new ObjectCreationAnalyzer();

        [Test]
        public static void ArgumentExceptionWithMessageAndNameof()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo(object o)
        {
            throw new ArgumentException(""message"", nameof(o));
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ArgumentNullExceptionWithMessageAndNameof()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo(object o)
        {
            throw new ArgumentNullException(nameof(o), ""message"");
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void ArgumentOutOfRangeExceptionWithMessageAndNameof()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo(object o)
        {
            throw new ArgumentOutOfRangeException(nameof(o), ""message"");
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }
    }
}
