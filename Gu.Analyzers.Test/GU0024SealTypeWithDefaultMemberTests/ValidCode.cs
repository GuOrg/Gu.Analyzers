namespace Gu.Analyzers.Test.GU0024SealTypeWithDefaultMemberTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ClassDeclarationAnalyzer();

        [Test]
        public static void WhenSealedWithProperty()
        {
            var code = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        public static Foo Default { get; } = new Foo();
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenSealedWithField()
        {
            var code = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        public static readonly Foo Default = new Foo();
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenNoDefaultField()
        {
            var code = @"
namespace RoslynSandbox
{
    public class Foo
    {
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
