namespace Gu.Analyzers.Test.CodeFixes
{
    using System.Collections.Immutable;
    using Gu.Analyzers.CodeFixes;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class AddNullableAttributeFixTests
    {
        private static readonly CodeFixProvider Fix = new AddNullableAttributeFix();
        private static readonly ExpectedDiagnostic CS8625 = ExpectedDiagnostic.Create("CS8625");
        private static readonly ExpectedDiagnostic CS8653 = ExpectedDiagnostic.Create("CS8653");
        private static readonly CSharpCompilationOptions CompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable);

        [Test]
        public static void AddNotNullWhenAndUsing()
        {
            var before = @"
namespace N
{
    public static class C
    {
        public static bool Try(string s, out string result)
        {
            if (s.Length > 2)
            {
                result = s;
                return true;
            }

            result = ↓null;
            return false;
        }
    }
}";

            var after = @"
namespace N
{
    using System.Diagnostics.CodeAnalysis;

    public static class C
    {
        public static bool Try(string s, [NotNullWhen(true)] out string? result)
        {
            if (s.Length > 2)
            {
                result = s;
                return true;
            }

            result = null;
            return false;
        }
    }
}";
            RoslynAssert.CodeFix(Fix, CS8625, before, after, compilationOptions: CompilationOptions);
        }

        [Test]
        public static void AddNotNullWhenTrue()
        {
            var before = @"
namespace N
{
    using System.Diagnostics.CodeAnalysis;

    public static class C
    {
        public static bool Try(string s, out string result)
        {
            _ = typeof(NotNullWhenAttribute);
            if (s.Length > 2)
            {
                result = s;
                return true;
            }

            result = ↓null;
            return false;
        }
    }
}";

            var after = @"
namespace N
{
    using System.Diagnostics.CodeAnalysis;

    public static class C
    {
        public static bool Try(string s, [NotNullWhen(true)] out string? result)
        {
            _ = typeof(NotNullWhenAttribute);
            if (s.Length > 2)
            {
                result = s;
                return true;
            }

            result = null;
            return false;
        }
    }
}";
            RoslynAssert.CodeFix(Fix, CS8625, before, after, compilationOptions: CompilationOptions);
        }

        [Test]
        public static void AddMaybeNullWhenFalse()
        {
            var before = @"
namespace N
{
    public static class C
    {
        public static bool Try<T>(T s, bool b, out T result)
        {
            if (b)
            {
                result = s;
                return true;
            }

            result = ↓default;
            return false;
        }
    }
}";

            var after = @"
namespace N
{
    using System.Diagnostics.CodeAnalysis;

    public static class C
    {
        public static bool Try<T>(T s, bool b, [MaybeNullWhen(false)] out T result)
        {
            if (b)
            {
                result = s;
                return true;
            }

            result = default;
            return false;
        }
    }
}";
            RoslynAssert.CodeFix(Fix, CS8653, before, after, compilationOptions: CompilationOptions);
        }
    }
}
