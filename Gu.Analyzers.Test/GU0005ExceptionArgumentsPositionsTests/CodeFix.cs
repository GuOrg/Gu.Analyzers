namespace Gu.Analyzers.Test.GU0005ExceptionArgumentsPositionsTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class CodeFix : CodeFixVerifier<GU0005ExceptionArgumentsPositions, MoveArgumentCodeFixProvider>
    {
        [TestCase(@"throw new ArgumentException(↓nameof(o), ""message"");", @"throw new ArgumentException(""message"", nameof(o));")]
        [TestCase(@"throw new ArgumentException(↓""o"", ""message"");", @"throw new ArgumentException(""message"", ""o"");")]
        [TestCase(@"throw new ArgumentException(↓""o"", ""message"", new Exception());", @"throw new ArgumentException(""message"", ""o"", new Exception());")]
        [TestCase(@"throw new ArgumentException(↓""o"", ""message"", new Exception());", @"throw new ArgumentException(""message"", ""o"", new Exception());")]
        [TestCase(@"throw new ArgumentNullException(""Meh"", ↓nameof(o));", @"throw new ArgumentNullException(nameof(o), ""Meh"");")]
        public async Task WhenThrowing(string error, string @fixed)
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
            testCode = testCode.AssertReplace(@"throw new ArgumentException(↓nameof(o), ""message"");", error);
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Use correct argument positions.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

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
            fixedCode = fixedCode.AssertReplace(@"throw new ArgumentException(""message"", nameof(o));", @fixed);
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}
