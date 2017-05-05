namespace Gu.Analyzers.Test.GU0060EnumMemberValueConflictsWithAnother
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class Diagnostics : DiagnosticVerifier<Analyzers.GU0060EnumMemberValueConflictsWithAnother>
    {
        [Test]
        public async Task ExplicitValueSharing()
        {
            var testCode = @"
using System;

[Flags]
public enum Bad2
{
    A = 1,
    B = 2,
    ↓Baaaaaaad = 2
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
using System;

[Flags]
public enum Bad
{
    A = 1,
    B = 2,
    ↓Baaaaaaad = 3
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Enum member value conflicts with another.");
            await this.VerifyCSharpDiagnosticAsync(new[] { testCode }, expected)
                      .ConfigureAwait(false);
        }
    }
}