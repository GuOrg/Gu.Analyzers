namespace Gu.Analyzers.Test.GU0033DontIgnoreReturnValueOfTypeIDisposableTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class CodeFixAddUsing : CodeFixVerifier<GU0033DontIgnoreReturnValueOfTypeIDisposable, AddUsingCodeFixProvider>
    {
        [Test]
        public async Task AddUsingForIgnoredReturn()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo
{
    public void Meh()
    {
        ↓File.OpenRead(string.Empty);
        var i = 1;
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore returnvalue of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo
{
    public void Meh()
    {
        using (File.OpenRead(string.Empty))
        {
            var i = 1;
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}