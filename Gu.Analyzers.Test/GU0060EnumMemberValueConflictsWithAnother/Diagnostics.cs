namespace Gu.Analyzers.Test.GU0060EnumMemberValueConflictsWithAnother
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class Diagnostics : DiagnosticVerifier<Analyzers.GU0060EnumMemberValueConflictsWithAnother>
    {
        [Test]
        public async Task ImplicitValueSharing()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    [Flags]
    public enum Bad
    {
        None,
        A,
        B,
        ↓Baaaaaaad
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Enum member value conflicts with another.");
            await this.VerifyCSharpDiagnosticAsync(new[] { testCode }, expected)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ExplicitValueSharing()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    [Flags]
    public enum Bad2
    {
        A = 1,
        B = 2,
        ↓Baaaaaaad = 2
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Enum member value conflicts with another.");
            await this.VerifyCSharpDiagnosticAsync(new[] { testCode }, expected)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ExplicitValueSharingWithBitwiseSum()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    [Flags]
    public enum Bad
    {
        A = 1,
        B = 2,
        ↓Baaaaaaad = 3
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Enum member value conflicts with another.");
            await this.VerifyCSharpDiagnosticAsync(new[] { testCode }, expected)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ExplicitValueSharingDifferentBases()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    [Flags]
    public enum Bad
    {
        A = 1,
        B = 2,
        C = 4,
        D = 8,
        E = 16,
        F = 32,
        ↓Baaaaaaad = 0x0F
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Enum member value conflicts with another.");
            await this.VerifyCSharpDiagnosticAsync(new[] { testCode }, expected)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ExplicitValueSharingBitshifts()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    [Flags]
    public enum Bad
    {
        A = 1 << 0,
        B = 1 << 1,
        C = 1 << 2,
        ↓Baaaaaaad = 1 << 2
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Enum member value conflicts with another.");
            await this.VerifyCSharpDiagnosticAsync(new[] { testCode }, expected)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ExplicitValueSharingNonFlag()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public enum Bad
    {
        A,
        B,
        C,
        ↓Baaaaaaad = 2
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Enum member value conflicts with another.");
            await this.VerifyCSharpDiagnosticAsync(new[] { testCode }, expected)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ExplicitValueSharingPartial()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    [Flags]
    public enum Bad
    {
        A = 1,
        B = 2,
        ↓Baaaaaaad = A | 2
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Enum member value conflicts with another.");
            await this.VerifyCSharpDiagnosticAsync(new[] { testCode }, expected)
                      .ConfigureAwait(false);
        }
    }
}