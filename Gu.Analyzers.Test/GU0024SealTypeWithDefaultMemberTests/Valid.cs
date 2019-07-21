namespace Gu.Analyzers.Test.GU0024SealTypeWithDefaultMemberTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class Valid
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ClassDeclarationAnalyzer();

        [Test]
        public static void WhenSealedWithProperty()
        {
            var code = @"
namespace N
{
    public sealed class C
    {
        public static C Default { get; } = new C();
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenSealedWithField()
        {
            var code = @"
namespace N
{
    public sealed class C
    {
        public static readonly C Default = new C();
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenNoDefaultField()
        {
            var code = @"
namespace N
{
    public class C
    {
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
