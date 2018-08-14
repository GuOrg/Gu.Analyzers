namespace Gu.Analyzers.Test.GU0024SealTypeWithDefaultMemberTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ClassDeclarationAnalyzer();

        [Test]
        public void WhenSealedWithProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        public static Foo Default { get; } = new Foo();
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenSealedWithField()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        public static readonly Foo Default = new Foo();
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenNoDefaultField()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
