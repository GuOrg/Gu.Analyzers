namespace Gu.Analyzers.Test.GU0005ExceptionArgumentsPositionsTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class CodeFix
    {
        private static readonly ObjectCreationAnalyzer Analyzer = new ObjectCreationAnalyzer();
        private static readonly MoveArgumentFix Fix = new MoveArgumentFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0005ExceptionArgumentsPositions.Descriptor);

        [TestCase(@"throw new ArgumentException(↓nameof(o), ""message"");", @"throw new ArgumentException(""message"", nameof(o));")]
        [TestCase(@"throw new System.ArgumentException(↓nameof(o), ""message"");", @"throw new System.ArgumentException(""message"", nameof(o));")]
        [TestCase(@"throw new ArgumentException(↓""o"", ""message"");", @"throw new ArgumentException(""message"", ""o"");")]
        [TestCase(@"throw new ArgumentException(↓""o"", ""message"", new Exception());", @"throw new ArgumentException(""message"", ""o"", new Exception());")]
        [TestCase(@"throw new ArgumentNullException(""Meh"", ↓nameof(o));", @"throw new ArgumentNullException(nameof(o), ""Meh"");")]
        public static void WhenThrowing(string error, string @fixed)
        {
            var before = @"
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
}".AssertReplace(@"throw new ArgumentException(↓nameof(o), ""message"");", error);

            var after = @"
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
}".AssertReplace(@"throw new ArgumentException(""message"", nameof(o));", @fixed);

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void AliasedInside()
        {
            var before = @"
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

            var after = @"
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void AliasedOutside()
        {
            var before = @"
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

            var after = @"
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }
    }
}
