namespace Gu.Analyzers.Test.GU0005ExceptionArgumentsPositionsTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class CodeFix
    {
        private static readonly ObjectCreationAnalyzer Analyzer = new ObjectCreationAnalyzer();
        private static readonly MoveArgumentCodeFixProvider Fix = new MoveArgumentCodeFixProvider();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0005");

        [TestCase(@"throw new ArgumentException(↓nameof(o), ""message"");", @"throw new ArgumentException(""message"", nameof(o));")]
        [TestCase(@"throw new System.ArgumentException(↓nameof(o), ""message"");", @"throw new System.ArgumentException(""message"", nameof(o));")]
        [TestCase(@"throw new ArgumentException(↓""o"", ""message"");", @"throw new ArgumentException(""message"", ""o"");")]
        [TestCase(@"throw new ArgumentException(↓""o"", ""message"", new Exception());", @"throw new ArgumentException(""message"", ""o"", new Exception());")]
        [TestCase(@"throw new ArgumentNullException(""Meh"", ↓nameof(o));", @"throw new ArgumentNullException(nameof(o), ""Meh"");")]
        public void WhenThrowing(string error, string @fixed)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo(object o)
        {
            throw new ArgumentException(↓nameof(o), ""message"");
        }
    }
}";

            var fixedCode = @"
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
            testCode = testCode.AssertReplace(@"throw new ArgumentException(↓nameof(o), ""message"");", error);
            fixedCode = fixedCode.AssertReplace(@"throw new ArgumentException(""message"", nameof(o));", @fixed);
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void AliasedInside()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using Meh = System.ArgumentException;

    public class Foo
    {
        public Foo(object o)
        {
            throw new Meh(↓nameof(o), ""message"");
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using Meh = System.ArgumentException;

    public class Foo
    {
        public Foo(object o)
        {
            throw new Meh(""message"", nameof(o));
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void AliasedOutside()
        {
            var testCode = @"
using Meh = System.ArgumentException;
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(object o)
        {
            throw new Meh(↓nameof(o), ""message"");
        }
    }
}";

            var fixedCode = @"
using Meh = System.ArgumentException;
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(object o)
        {
            throw new Meh(""message"", nameof(o));
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }
    }
}
