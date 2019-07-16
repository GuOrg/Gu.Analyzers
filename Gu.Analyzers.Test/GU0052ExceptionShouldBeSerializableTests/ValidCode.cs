namespace Gu.Analyzers.Test.GU0052ExceptionShouldBeSerializableTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new GU0052ExceptionShouldBeSerializable();

        [Test]
        public static void WhenNoBaseClass()
        {
            var code = @"
namespace N
{
public class Foo
{
}
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenSerializable()
        {
            var code = @"
namespace N
{
    using System;

    [Serializable]
    public class FooException : Exception
    {
        public FooException()
            : base(string.Empty)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenSerializableAndObsoleteSameList()
        {
            var code = @"
namespace N
{
    using System;

    [Serializable, Obsolete]
    public class FooException : Exception
    {
        public FooException()
            : base(string.Empty)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenSerializableAndObsoleteDifferentLists()
        {
            var code = @"
namespace N
{
    using System;

    [Serializable]
    [Obsolete]
    public class FooException : Exception
    {
        public FooException()
            : base(string.Empty)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ExtendedWithAttribute()
        {
            var code = @"
namespace N
{
    using System;
    
    [Serializable]
    public class FooException : Exception
    {
        public FooException()
        : base(string.Empty)
        {
        }
    }

    [Serializable]
    public class BarException : FooException
    {
        public BarException()
        : base()
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
