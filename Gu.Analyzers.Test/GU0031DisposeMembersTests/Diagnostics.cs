namespace Gu.Analyzers.Test.GU0031DisposeMembersTests
{
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class Diagnostics : DiagnosticVerifier<GU0031DisposeMembers>
    {
        [Test]
        public async Task NotDisposingFieldInDisposeMethod()
        {
            var testCode = @"
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓private readonly Stream stream = File.OpenRead("""");
        
        public void Dispose()
        {
        }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose members.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingFieldInDisposeMethod2()
        {
            var testCode = @"
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        private readonly Stream stream1 = File.OpenRead("""");
        ↓private readonly Stream stream2 = File.OpenRead("""");
        
        public void Dispose()
        {
            stream1.Dispose();
        }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose members.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingFieldOfTypeObjectInDisposeMethod()
        {
            var testCode = @"
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓private readonly object stream = File.OpenRead("""");
        
        public void Dispose()
        {
        }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose members.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingFieldWhenContainingTypeIsNotIDisposable()
        {
            var testCode = @"
    using System;
    using System.IO;

    public sealed class Foo
    {
        ↓private readonly Stream stream = File.OpenRead("""");
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose members.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingPropertyWhenInitializedInline()
        {
            var testCode = @"
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓public Stream Stream { get; set; } = File.OpenRead("""");
        
        public void Dispose()
        {
        }
    }";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose members.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingPropertyOfTypeObjectWhenInitializedInline()
        {
            var testCode = @"
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        ↓public object Stream { get; set; } = File.OpenRead("""");
        
        public void Dispose()
        {
        }
    }";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose members.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
        }

        [Test]
        public async Task NotDisposingPropertyWhenInitializedInCtor()
        {
            var testCode = @"
    using System;
    using System.IO;

    public sealed class Foo : IDisposable
    {
        public Foo()
        {
            this.Stream = File.OpenRead("""");
        }

        ↓public Stream Stream { get; set; }
        
        public void Dispose()
        {
        }
    }";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose members.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
        }
    }
}