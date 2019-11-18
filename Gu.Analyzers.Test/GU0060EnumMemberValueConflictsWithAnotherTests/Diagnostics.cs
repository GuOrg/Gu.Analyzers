namespace Gu.Analyzers.Test.GU0060EnumMemberValueConflictsWithAnotherTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Diagnostics
    {
        private static readonly GU0060EnumMemberValueConflictsWithAnother Analyzer = new GU0060EnumMemberValueConflictsWithAnother();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0060EnumMemberValueConflictsWithAnother);

        [Test]
        public static void ImplicitValueSharing()
        {
            var code = @"
namespace N
{
    using System;

    [Flags]
    public enum E
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
                code: code,
                cleanedSources: out code);
            RoslynAssert.Diagnostics(Analyzer, expectedDiagnostic, code);
        }

        [Test]
        public static void ExplicitValueSharing()
        {
            var code = @"
namespace N
{
    using System;

    [Flags]
    public enum E
    {
        A = 1,
        B = 2,
        ↓Bad = 2
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void ExplicitValueSharingWithBitwiseSum()
        {
            var code = @"
namespace N
{
    using System;

    [Flags]
    public enum E
    {
        A = 1,
        B = 2,
        ↓Bad = 3
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void ExplicitValueSharingDifferentBases()
        {
            var code = @"
namespace N
{
    using System;

    [Flags]
    public enum E
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
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void ExplicitValueSharingBitshifts()
        {
            var code = @"
namespace N
{
    using System;

    [Flags]
    public enum E
    {
        A = 1 << 0,
        B = 1 << 1,
        C = 1 << 2,
        ↓Bad = 1 << 2
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void ExplicitValueSharingNonFlag()
        {
            var code = @"
namespace N
{
    using System;

    public enum E
    {
        A,
        B,
        C,
        ↓Bad = 2
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void ExplicitValueSharingPartial()
        {
            var code = @"
namespace N
{
    using System;

    [Flags]
    public enum E
    {
        A = 1,
        B = 2,
        ↓Bad = A | 2
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void ExplicitValueSharingPartial2()
        {
            var code = @"
namespace N
{
    using System;

    [Flags]
    public enum E
    {
        A = 1,
        B = 2,
        ↓Bad = 2 | A,
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }
    }
}
