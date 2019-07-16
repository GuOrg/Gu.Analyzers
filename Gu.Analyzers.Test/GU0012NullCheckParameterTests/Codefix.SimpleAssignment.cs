namespace Gu.Analyzers.Test.GU0012NullCheckParameterTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static partial class CodeFix
    {
        internal static class SimpleAssignment
        {
            private static readonly SimpleAssignmentAnalyzer Analyzer = new SimpleAssignmentAnalyzer();
            private static readonly NullCheckParameterFix Fix = new NullCheckParameterFix();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0012");

            [TestCase("public")]
            [TestCase("internal")]
            [TestCase("protected")]
            public static void ConstructorFullyQualified(string access)
            {
                var before = @"
namespace N
{
    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = ↓text;
        }
    }
}".AssertReplace("public C", $"{access} C");

                var after = @"
namespace N
{
    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = text ?? throw new System.ArgumentNullException(nameof(text));
        }
    }
}".AssertReplace("public C", $"{access} C");

                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void PublicCtor()
            {
                var before = @"
namespace N
{
    using System;

    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = ↓text;
        }
    }
}";

                var after = @"
namespace N
{
    using System;

    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = text ?? throw new ArgumentNullException(nameof(text));
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void PublicCtorOutParameter()
            {
                var before = @"
namespace N
{
    using System;

    public class C
    {
        private readonly string text;

        public C(string text, out string result)
        {
            this.text = ↓text;
            result = string.Empty;
        }
    }
}";

                var after = @"
namespace N
{
    using System;

    public class C
    {
        private readonly string text;

        public C(string text, out string result)
        {
            this.text = text ?? throw new ArgumentNullException(nameof(text));
            result = string.Empty;
        }
    }
}";
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }
        }
    }
}
