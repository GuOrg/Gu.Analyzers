namespace Gu.Analyzers.Test.GU0090DontThrowNotImplementedExceptionTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Diagnostics
    {
        private static readonly ExceptionAnalyzer Analyzer = new ExceptionAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0090DontThrowNotImplementedException.Descriptor);

        [Test]
        public static void ExceptionThrownInsideMethodBlock()
        {
            var code = @"
namespace N
{
    class Foo
    {
        void Method()
        {
            throw new System.NotImplementedException();
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void ExceptionThrownInline()
        {
            var code = @"
namespace N
{
    class Foo
    {
        int Method() => throw new System.NotImplementedException();
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void ExceptionNullCoalescing()
        {
            var code = @"
namespace N
{
    class Foo
    {
        void Method()
        {
            int? integer = null;
            
            int nonNull = integer ?? throw new System.NotImplementedException();
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }
    }
}
