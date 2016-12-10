#pragma warning disable 1998
namespace Gu.Analyzers.Test.GU0031DisposeMemberTests
{
    using System.Threading;
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class CodeFixImplementIDisposableSealed : CodeFixVerifier<GU0031DisposeMember, ImplementIDisposableSealedCodeFixProvider>
    {
        [Test]
        public async Task ImplementIDisposable()
        {
            var testCode = @"
using System;
using System.IO;

public class Foo
{
    ↓private readonly Stream stream = File.OpenRead("""");
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None)
                      .ConfigureAwait(false);

            //            var fixedCode = @"
            //using System;
            //using System.IO;

            //public sealed class Foo
            //: IDisposable
            //{
            //    private readonly Stream stream = File.OpenRead("""");

            //    public void Dispose()
            //    {
            //    }
            //}";
            //            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true)
            //                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposableDisposeMethod()
        {
//            var testCode = @"
//using System;

//public class Foo : ↓IDisposable
//{
//}";
//            var expected = this.CSharpDiagnostic("CS0535")
//                               .WithLocationIndicated(ref testCode);
//            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
//                      .ConfigureAwait(false);

//            var fixedCode = @"
//using System;

//public sealed class Foo : IDisposable
//{
//    public void Dispose()
//    {
//    }
//}";
//            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true)
//                      .ConfigureAwait(false);
        }
    }
}