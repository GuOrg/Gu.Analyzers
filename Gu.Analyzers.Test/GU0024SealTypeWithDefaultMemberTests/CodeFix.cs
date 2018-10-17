namespace Gu.Analyzers.Test.GU0024SealTypeWithDefaultMemberTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ClassDeclarationAnalyzer();
        private static readonly CodeFixProvider Fix = new MakeSealedFix();

        [Test]
        public void Field()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class ↓Foo
    {
        public static readonly Foo Default = new Foo();
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        public static readonly Foo Default = new Foo();
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, testCode, fixedCode, fixTitle: "Make sealed.");
        }

        [Test]
        public void Property()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class ↓Foo
    {
        public static Foo Default { get; } = new Foo();
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        public static Foo Default { get; } = new Foo();
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, testCode, fixedCode, fixTitle: "Make sealed.");
        }
    }
}
