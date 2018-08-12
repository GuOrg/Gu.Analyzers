namespace Gu.Analyzers.Test.GU0023StaticMemberOrderTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal class Diagnostics
    {
        private static readonly DiagnosticAnalyzer Analyzer = new GU0023StaticMemberOrderAnalyzer();

        [Test]
        public void Message()
        {
            var code = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public static readonly int Value1 = ↓Value2;

        public static readonly int Value2 = 2;
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic.Create("GU0023", "Member 'RoslynSandbox.Foo.Value2' must be declared before 'RoslynSandbox.Foo.Value1'"), code);
        }

        [Test]
        public void FieldInitializedWithField()
        {
            var code = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public static readonly int Value1 = ↓Value2;

        public static readonly int Value2 = 2;
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, code);
        }

        [Test]
        public void PropertyInitializedWithProperty()
        {
            var code = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public static int Value1 { get; } = ↓Value2;

        public static int Value2 { get; } = 2;
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, code);
        }

        [Test]
        public void FieldInitializedWithFieldViaMethod()
        {
            var code = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public static readonly int Value1 = ↓Id(Value2);

        public static readonly int Value2 = 2;

        private static int Id(int value) => value;
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, code);
        }

        [Test]
        public void DefaultFieldUsingStaticField()
        {
            var code = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        public static readonly Foo Default = ↓new Foo();
        
        private static readonly string text = ""abc"";
        
        public string Text { get; set; } = text;
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, code);
        }
    }
}
