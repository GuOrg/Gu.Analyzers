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
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("CS8625");

        [Test]
        public static void Simple()
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
    public static class C
    {
        public static bool Try(string s, [NotNullWhen(true)]out string? result)
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
            RoslynAssert.CodeFix(Fix, ExpectedDiagnostic, before, after, compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        }
    }
}
