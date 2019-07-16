namespace Gu.Analyzers.Test.GU0023StaticMemberOrderTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class Diagnostics
    {
        private static readonly DiagnosticAnalyzer Analyzer = new GU0023StaticMemberOrderAnalyzer();

        [Test]
        public static void Message()
        {
            var code = @"
namespace N
{
    public class Foo
    {
        public static readonly int Value1 = ↓Value2;

        public static readonly int Value2 = 2;
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic.Create("GU0023", "Member 'N.Foo.Value2' must be declared before 'N.Foo.Value1'"), code);
        }

        [Test]
        public static void StaticFieldInitializedWithField()
        {
            var code = @"
namespace N
{
    public class Foo
    {
        public static readonly int Value1 = ↓Value2;

        public static readonly int Value2 = 2;
    }
}";
            RoslynAssert.Diagnostics(Analyzer, code);
        }

        [Test]
        public static void ConstFieldInitializedWithField()
        {
            var code = @"
namespace N
{
    public class Foo
    {
        public const int Value1 = ↓Value2;

        public const int Value2 = 2;
    }
}";
            RoslynAssert.Diagnostics(Analyzer, code);
        }

        [Test]
        public static void PropertyInitializedWithProperty()
        {
            var code = @"
namespace N
{
    public class Foo
    {
        public static int Value1 { get; } = ↓Value2;

        public static int Value2 { get; } = 2;
    }
}";
            RoslynAssert.Diagnostics(Analyzer, code);
        }

        [Test]
        public static void FieldInitializedWithFieldViaMethod()
        {
            var code = @"
namespace N
{
    public class Foo
    {
        public static readonly int Value1 = ↓Id(Value2);

        public static readonly int Value2 = 2;

        private static int Id(int value) => value;
    }
}";
            RoslynAssert.Diagnostics(Analyzer, code);
        }

        [Test]
        public static void DefaultFieldUsingStaticField()
        {
            var code = @"
namespace N
{
    public sealed class Foo
    {
        public static readonly Foo Default = ↓new Foo();
        
        private static readonly string text = ""abc"";
        
        public string Text { get; set; } = text;
    }
}";
            RoslynAssert.Diagnostics(Analyzer, code);
        }
    }
}
