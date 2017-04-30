namespace Gu.Analyzers.Test.GU0033DontIgnoreReturnValueOfTypeIDisposableTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class CodeFixAddToCompositeDisposable : CodeFixVerifier<GU0033DontIgnoreReturnValueOfTypeIDisposable, AddToCompositeDisposableCodeFixProvider>
    {
        [Test]
        public async Task AssignIgnoredReturnValueToCompositeDisposableCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class Foo
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        internal Foo()
        {
            ↓File.OpenRead(string.Empty);
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore returnvalue of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    internal sealed class Foo
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        internal Foo()
        {
            this.disposable.Add(File.OpenRead(string.Empty));
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}