namespace Gu.Analyzers.Test.GU0052ExceptionShouldBeSerializableTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class Diagnostics
    {
        private static readonly DiagnosticAnalyzer Analyzer = new GU0052ExceptionShouldBeSerializable();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0052ExceptionShouldBeSerializable.DiagnosticId);

        [Test]
        public static void WhenNoAttribute()
        {
            var code = @"
namespace RoslynSandbox
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
namespace RoslynSandbox
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
}
