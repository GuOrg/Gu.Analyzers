namespace Gu.Analyzers.Test.GU0060EnumMemberValueConflictsWithAnotherTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        [Test]
        public void ImplicitValueSharing()
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
        ↓Bad
    }
}";

            var expectedDiagnostic = ExpectedDiagnostic.CreateFromCodeWithErrorsIndicated(
                diagnosticId: "GU0060",
                message: "Enum member value conflicts with another.",
                code: testCode,
                cleanedSources: out testCode);
            AnalyzerAssert.Diagnostics<GU0060EnumMemberValueConflictsWithAnother>(expectedDiagnostic, testCode);
        }

        [Test]
        public void ExplicitValueSharing()
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
        ↓Bad = 2
    }
}";
            AnalyzerAssert.Diagnostics<GU0060EnumMemberValueConflictsWithAnother>(testCode);
        }

        [Test]
        public void ExplicitValueSharingWithBitwiseSum()
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
        ↓Bad = 3
    }
}";
            AnalyzerAssert.Diagnostics<GU0060EnumMemberValueConflictsWithAnother>(testCode);
        }

        [Test]
        public void ExplicitValueSharingDifferentBases()
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
        ↓Bad = 0x0F
    }
}";
            AnalyzerAssert.Diagnostics<GU0060EnumMemberValueConflictsWithAnother>(testCode);
        }

        [Test]
        public void ExplicitValueSharingBitshifts()
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
        ↓Bad = 1 << 2
    }
}";
            AnalyzerAssert.Diagnostics<GU0060EnumMemberValueConflictsWithAnother>(testCode);
        }

        [Test]
        public void ExplicitValueSharingNonFlag()
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
        ↓Bad = 2
    }
}";
            AnalyzerAssert.Diagnostics<GU0060EnumMemberValueConflictsWithAnother>(testCode);
        }

        [Test]
        public void ExplicitValueSharingPartial()
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
        ↓Bad = A | 2
    }
}";
            AnalyzerAssert.Diagnostics<GU0060EnumMemberValueConflictsWithAnother>(testCode);
        }
    }
}