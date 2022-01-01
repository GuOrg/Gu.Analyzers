namespace Gu.Analyzers.Test.GU0052ExceptionShouldBeSerializableTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class Diagnostics
{
    private static readonly GU0052ExceptionShouldBeSerializable Analyzer = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0052ExceptionShouldBeSerializable);

    [Test]
    public static void WhenNoAttribute()
    {
        var code = @"
namespace N
{
    using System;
    
    public class ↓FooException : Exception
    {
        public FooException()
        : base(string.Empty)
        {
        }
    }
}";
        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
    }

    [Test]
    public static void ExtendedNoAttribute()
    {
        var code = @"
namespace N
{
    using System;
    
    [Serializable]
    public class FooException : Exception
    {
        public FooException(string text)
            : base(text)
        {
        }
    }

    public class ↓BarException : FooException
    {
        public BarException()
            : base(string.Empty)
        {
        }
    }
}";
        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
    }
}