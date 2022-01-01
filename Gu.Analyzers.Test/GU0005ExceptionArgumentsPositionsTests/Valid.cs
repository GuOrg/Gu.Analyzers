namespace Gu.Analyzers.Test.GU0005ExceptionArgumentsPositionsTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class Valid
{
    private static readonly ObjectCreationAnalyzer Analyzer = new();

    [Test]
    public static void ArgumentExceptionWithMessageAndNameof()
    {
        var code = @"
namespace N
{
    using System;

    public class C
    {
        public C(object o)
        {
            throw new ArgumentException(""message"", nameof(o));
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ArgumentNullExceptionWithMessageAndNameof()
    {
        var code = @"
namespace N
{
    using System;

    public class C
    {
        public C(object o)
        {
            throw new ArgumentNullException(nameof(o), ""message"");
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ArgumentOutOfRangeExceptionWithMessageAndNameof()
    {
        var code = @"
namespace N
{
    using System;

    public class C
    {
        public C(object o)
        {
            throw new ArgumentOutOfRangeException(nameof(o), ""message"");
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }
}