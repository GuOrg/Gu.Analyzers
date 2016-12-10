namespace Gu.Analyzers.Test.GU0031DisposeMemberTests
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    using NUnit.Framework;

    internal class CodeFixImplementIDisposableSealed : CodeFixVerifier<GU0031DisposeMember, ImplementIDisposableSealedCodeFixProvider>
    {
        [Test]
        [Explicit("Testfixture broken here.")]
        public async Task ImplementIDisposable()
        {
            var testCode = @"
using System;
using System.IO;

public class Foo
{
    ↓private readonly Stream stream = File.OpenRead("""");
}";
            var expected = this.CSharpDiagnostic(GU0031DisposeMember.DiagnosticId)
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Dispose member.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None)
                      .ConfigureAwait(false);

            var fixedCode = @"
using System;
using System.IO;

public sealed class Foo : IDisposable
{
    private readonly Stream stream = File.OpenRead("""");

    public void Dispose()
    {
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ImplementIDisposableDisposeMethod()
        {
            var testCode = @"
using System;

public class Foo : ↓IDisposable
{
}";
            var expected = this.CSharpDiagnostic("CS0535")
                               .WithLocationIndicated(ref testCode);
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"
using System;

public sealed class Foo : IDisposable
{
    public void Dispose()
    {
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true)
                      .ConfigureAwait(false);
        }

        [Test]
        [Explicit]
        public async Task ImplementIDisposableDisposeMethodWithProtectedProperty()
        {
            var testCode = @"
using System;

public class Foo : ↓IDisposable
{
    protected int Value { get; private set; }
}";
            var expected = this.CSharpDiagnostic("CS0535")
                               .WithLocationIndicated(ref testCode);
            await this.VerifyCSharpDiagnosticAsync(testCode, expected)
                      .ConfigureAwait(false);

            var fixedCode = @"
using System;

public sealed class Foo : IDisposable
{
    private int Value { get; set; }

    public void Dispose()
    {
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true)
                      .ConfigureAwait(false);
        }

        internal override IEnumerable<DiagnosticAnalyzer> GetCSharpDiagnosticAnalyzers()
        {
            return base.GetCSharpDiagnosticAnalyzers()
                       .Concat(new[] {DummyAnalyzer.Default});
        }

        private class DummyAnalyzer : DiagnosticAnalyzer
        {
            public static readonly DummyAnalyzer Default = new DummyAnalyzer();

            private DummyAnalyzer()
            {
            }

            public override void Initialize(AnalysisContext context)
            {
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
                ImmutableArray.Create(
                    new DiagnosticDescriptor("CS0535",
                                             "",
                                             "'Foo' does not implement interface member 'IDisposable.Dispose()'",
                                             "",
                                             DiagnosticSeverity.Error, 
                                             false));
        }
    }
}