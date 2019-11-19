namespace Gu.Analyzers.Test.GU0075PreferReturnNullable
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ParameterAnalyzer();
        private static readonly CodeFixProvider Fix = new ReturnNullableFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0075PreferReturnNullable);

        [Test]
        public static void InstanceMethodSIngleOut()
        {
            var before = @"
#nullable enable
namespace N
{
    class C
    {
        bool M(↓out string? s)
        {
            if (nameof(C).Length > 1)
            {
                s = string.Empty;
                return true;
            }

            s = null;
            return false;
        }
    }
}";

            var after = @"
#nullable enable
namespace N
{
    class C
    {
        string? M()
        {
            if (nameof(C).Length > 1)
            {
                return string.Empty;
            }

            return null;
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void InstanceMethodLastOut()
        {
            var before = @"
#nullable enable
namespace N
{
    class C
    {
        bool M(string text, ↓out string? s)
        {
            if (text.Length > 1)
            {
                s = string.Empty;
                return true;
            }

            s = null;
            return false;
        }
    }
}";

            var after = @"
#nullable enable
namespace N
{
    class C
    {
        string? M(string text)
        {
            if (text.Length > 1)
            {
                return string.Empty;
            }

            return null;
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void Local()
        {
            var before = @"
#nullable enable
namespace N
{
    class C
    {
        void Outer()
        {
            bool M(↓out string? s)
            {
                if (nameof(C).Length > 1)
                {
                    s = string.Empty;
                    return true;
                }

                s = null;
                return false;
            }
        }
    }
}";

            var after = @"
#nullable enable
namespace N
{
    class C
    {
        void Outer()
        {
            string? M()
            {
                if (nameof(C).Length > 1)
                {
                    return string.Empty;
                }

                return null;
            }
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }
    }
}
