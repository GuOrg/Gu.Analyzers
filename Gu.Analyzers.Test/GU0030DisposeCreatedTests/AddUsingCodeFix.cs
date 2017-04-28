namespace Gu.Analyzers.Test.GU0030DisposeCreatedTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal partial class AddUsingCodeFix : CodeFixVerifier<GU0030DisposeCreated, AddUsingCodeFixProvider>
    {
        private static readonly string DisposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";

        [Test]
        public async Task AddUsing()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo
{
    public void Meh()
    {
        ↓var stream = File.OpenRead(string.Empty);
        var i = 1;
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose created.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo
{
    public void Meh()
    {
        using (var stream = File.OpenRead(string.Empty))
        {
            var i = 1;
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}