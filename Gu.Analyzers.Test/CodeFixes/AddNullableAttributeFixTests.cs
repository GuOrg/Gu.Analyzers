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
        private static readonly ExpectedDiagnostic CS8601 = ExpectedDiagnostic.Create("CS8601");
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
        public static void AddNotNullWhenTrueWhenAssigningOutParameterWithNullLiteral()
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
        public static void AddNotNullWhenTrueWhenAssigningOutParameterWithAs()
        {
            var before = @"
namespace N
{
    using System.Diagnostics.CodeAnalysis;

    public static class C
    {
        public static bool Try(object o, out string result)
        {
            _ = typeof(NotNullWhenAttribute);
            result = ↓o as string;
            return result != null;
        }
    }
}";

            var after = @"
namespace N
{
    using System.Diagnostics.CodeAnalysis;

    public static class C
    {
        public static bool Try(object o, [NotNullWhen(true)] out string? result)
        {
            _ = typeof(NotNullWhenAttribute);
            result = o as string;
            return result != null;
        }
    }
}";
            RoslynAssert.CodeFix(Fix, CS8601, before, after, compilationOptions: CompilationOptions);
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

            result = default!;
            return false;
        }
    }
}";
            RoslynAssert.CodeFix(Fix, CS8653, before, after, compilationOptions: CompilationOptions);
        }

        [Test]
        public static void AddMaybeNullWhenFalseDefaultOfT()
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

            result = ↓default(T);
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

            result = default!;
            return false;
        }
    }
}";
            RoslynAssert.CodeFix(Fix, CS8653, before, after, compilationOptions: CompilationOptions);
        }

        [Test]
        public static void MakeOptionalParemeterNullable()
        {
            var before = @"
namespace N
{
    public static class C
    {
        public static void M(string s = ↓null)
        {
        }
    }
}";

            var after = @"
namespace N
{
    public static class C
    {
        public static void M(string? s = null)
        {
        }
    }
}";
            RoslynAssert.CodeFix(Fix, CS8625, before, after, compilationOptions: CompilationOptions);
        }
    }
}
