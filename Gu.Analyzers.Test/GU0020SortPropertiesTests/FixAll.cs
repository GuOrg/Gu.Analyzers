namespace Gu.Analyzers.Test.GU0020SortPropertiesTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class FixAll
    {
        private static readonly GU0020SortProperties Analyzer = new GU0020SortProperties();
        private static readonly SortPropertiesFix Fix = new SortPropertiesFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0020");

        [Test]
        public static void WhenMutableBeforeGetOnlyFirst()
        {
            var before = @"
namespace N
{
    public class C
    {
        ↓public int P1 { get; set; }

        public int P2 { get; }

        public int P3 { get; }

        public int P4 { get; }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        public int P2 { get; }

        public int P3 { get; }

        public int P4 { get; }

        public int P1 { get; set; }
    }
}";
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenAMess()
        {
            var before = @"
namespace N
{
    public class C
    {
        ↓public int P1 { get; set; }

        ↓public int P2 { get; private set; }

        public int P3 { get; }

        public int P4 => P3;
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        public int P3 { get; }

        public int P4 => P3;

        public int P2 { get; private set; }

        public int P1 { get; set; }
    }
}";
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenAMess1WithDocs()
        {
            var before = @"
namespace N
{
    public class C
    {
        /// <summary>
        /// Docs for P1
        /// </summary>
        ↓public int P1 { get; set; }

        /// <summary>
        /// Docs for P2
        /// </summary>
        ↓public int P2 { get; private set; }

        /// <summary>
        /// Docs for P3
        /// </summary>
        public int P3 { get; }

        /// <summary>
        /// Docs for P4
        /// </summary>
        public int P4 => P3;
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        /// <summary>
        /// Docs for P3
        /// </summary>
        public int P3 { get; }

        /// <summary>
        /// Docs for P4
        /// </summary>
        public int P4 => P3;

        /// <summary>
        /// Docs for P2
        /// </summary>
        public int P2 { get; private set; }

        /// <summary>
        /// Docs for P1
        /// </summary>
        public int P1 { get; set; }
    }
}";
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void PreservesDocumentOrder()
        {
            Assert.Inconclusive("Fails on devops, move to issue #154");
            var before = @"
namespace RoslynSandbox
{
    public class C
    {
        ↓public int P1 { get; set; }

        public int P2 { get; private set; }

        ↓public int P3 { get; set; }

        public int P4 { get; private set; }
    }
}";

            var after = @"
namespace RoslynSandbox
{
    public class C
    {
        public int P2 { get; private set; }

        public int P4 { get; private set; }

        public int P1 { get; set; }

        public int P3 { get; set; }
    }
}";
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }
    }
}
