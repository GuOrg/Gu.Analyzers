namespace Gu.Analyzers.Test.GU0024SealTypeWithDefaultMemberTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ClassDeclarationAnalyzer();
        private static readonly CodeFixProvider Fix = new MakeSealedFix();

        [Test]
        public static void Field()
        {
            var before = @"
namespace N
{
    public class ↓Foo
    {
        public static readonly Foo Default = new Foo();
    }
}";

            var after = @"
namespace N
{
    public sealed class Foo
    {
        public static readonly Foo Default = new Foo();
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "Make sealed.");
        }

        [Test]
        public static void Property()
        {
            var before = @"
namespace N
{
    public class ↓Foo
    {
        public static Foo Default { get; } = new Foo();
    }
}";

            var after = @"
namespace N
{
    public sealed class Foo
    {
        public static Foo Default { get; } = new Foo();
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "Make sealed.");
        }
    }
}
