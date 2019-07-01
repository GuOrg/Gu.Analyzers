namespace Gu.Analyzers.Test.GU0060EnumMemberValueConflictsWithAnotherTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Diagnostics
    {
        private static readonly GU0060EnumMemberValueConflictsWithAnother Analyzer = new GU0060EnumMemberValueConflictsWithAnother();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0060EnumMemberValueConflictsWithAnother.DiagnosticId);

        [Test]
        public static void ImplicitValueSharing()
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
            RoslynAssert.Diagnostics(Analyzer, expectedDiagnostic, testCode);
        }

        [Test]
        public static void ExplicitValueSharing()
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void ExplicitValueSharingWithBitwiseSum()
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void ExplicitValueSharingDifferentBases()
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void ExplicitValueSharingBitshifts()
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void ExplicitValueSharingNonFlag()
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void ExplicitValueSharingPartial()
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
