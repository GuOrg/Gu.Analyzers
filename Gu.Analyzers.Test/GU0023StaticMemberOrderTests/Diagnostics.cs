namespace Gu.Analyzers.Test.GU0023StaticMemberOrderTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Diagnostics
    {
        private static readonly GU0023StaticMemberOrderAnalyzer Analyzer = new();

        [Test]
        public static void Message()
        {
            var code = @"
namespace N
{
    public class C
    {
        public static readonly int Value1 = ↓Value2;

        public static readonly int Value2 = 2;
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic.Create("GU0023", "Member 'N.C.Value2' must be declared before 'N.C.Value1'"), code);
        }

        [Test]
        public static void StaticFieldInitializedWithField()
        {
            var code = @"
namespace N
{
    public class C
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
    public class C
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
    public class C
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
    public class C
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
    public sealed class C
    {
        public static readonly C Default = ↓new C();
        
        private static readonly string text = ""abc"";
        
        public string Text { get; set; } = text;
    }
}";
            RoslynAssert.Diagnostics(Analyzer, code);
        }

        [Test]
        public static void PartialSameDocument()
        {
            var code = @"
namespace N
{
    partial class C
    {
       private static int F1 = ↓F2;
    }
    partial class C
    {
       private static int F2 = 1;
    }
}";
            RoslynAssert.Diagnostics(Analyzer, code);
        }

        [Test]
        public static void PartialDifferentDocuments()
        {
            var code1 = @"
namespace N
{
    partial class C
    {
       private static int F1 = ↓F2;
    }
}";

            var code2 = @"
namespace N
{
    partial class C
    {
       private static int F2 = 1;
    }
}";
            RoslynAssert.Diagnostics(Analyzer, code1, code2);
        }
    }
}
