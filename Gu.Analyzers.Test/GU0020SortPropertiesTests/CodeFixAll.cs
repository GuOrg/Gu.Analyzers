namespace Gu.Analyzers.Test.GU0020SortPropertiesTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class CodeFixAll
    {
        private static readonly GU0020SortProperties Analyzer = new GU0020SortProperties();
        private static readonly SortPropertiesFix Fix = new SortPropertiesFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0020");

        [Test]
        public static void WhenMutableBeforeGetOnlyFirst()
        {
            var before = @"
namespace RoslynSandbox
{
    public class Foo
    {
        ↓public int A { get; set; }

        public int B { get; }

        public int C { get; }

        public int D { get; }
    }
}";

            var after = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public int B { get; }

        public int C { get; }

        public int D { get; }

        public int A { get; set; }
    }
}";
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenAMess()
        {
            var before = @"
namespace RoslynSandbox
{
    public class Foo
    {
        ↓public int A { get; set; }

        ↓public int B { get; private set; }

        public int C { get; }

        public int D => C;
    }
}";

            var after = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public int C { get; }

        public int D => C;

        public int B { get; private set; }

        public int A { get; set; }
    }
}";
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenAMess1WithDocs()
        {
            var before = @"
namespace RoslynSandbox
{
    public class Foo
    {
        /// <summary>
        /// Docs for A
        /// </summary>
        ↓public int A { get; set; }

        /// <summary>
        /// Docs for B
        /// </summary>
        ↓public int B { get; private set; }

        /// <summary>
        /// Docs for C
        /// </summary>
        public int C { get; }

        /// <summary>
        /// Docs for D
        /// </summary>
        public int D => C;
    }
}";

            var after = @"
namespace RoslynSandbox
{
    public class Foo
    {
        /// <summary>
        /// Docs for C
        /// </summary>
        public int C { get; }

        /// <summary>
        /// Docs for D
        /// </summary>
        public int D => C;

        /// <summary>
        /// Docs for B
        /// </summary>
        public int B { get; private set; }

        /// <summary>
        /// Docs for A
        /// </summary>
        public int A { get; set; }
    }
}";
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void PreservesDocumentOrder()
        {
            var before = @"
namespace RoslynSandbox
{
    public class Foo
    {
        ↓public int A { get; set; }

        public int B { get; private set; }

        ↓public int C { get; set; }

        public int D { get; private set; }
    }
}";

            var after = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public int B { get; private set; }

        public int D { get; private set; }

        public int A { get; set; }

        public int C { get; set; }
    }
}";
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }
    }
}
