namespace Gu.Analyzers.Test.GU0005ExceptionArgumentsPositionsTests
{
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class CodeFix : CodeFixVerifier<GU0005ExceptionArgumentsPositions, MoveArgumentCodeFixProvider>
    {
        [TestCase(@"throw new ArgumentException(↓nameof(o), ""message"");", @"throw new ArgumentException(""message"", nameof(o));")]
        [TestCase(@"throw new ArgumentException(↓""o"", ""message"");", @"throw new ArgumentException(""message"", ""o"");")]
        [TestCase(@"throw new ArgumentException(↓""o"", ""message"", new Exception());", @"throw new ArgumentException(""message"", ""o"", new Exception());")]
        public async Task WhenThrowingArgumentException(string error, string @fixed)
        {
            var testCode = @"
    using System;

    public class Foo
    {
        public Foo(object o)
        {
            throw new ArgumentException(↓nameof(o), ""message"");
        }
    }";
            testCode = testCode.AssertReplace(@"throw new ArgumentException(↓nameof(o), ""message"");", error);
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Use correct argument positions.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
    using System;

    public class Foo
    {
        public Foo(object o)
        {
            throw new ArgumentException(""message"", nameof(o));
        }
    }";
            fixedCode = fixedCode.AssertReplace(@"throw new ArgumentException(""message"", nameof(o));", @fixed);
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}
