namespace Gu.Analyzers.Test.CodeFixes
{
    using Gu.Roslyn.Asserts;

    using Microsoft.CodeAnalysis.CodeFixes;

    using NUnit.Framework;

    internal static partial class MakeStaticFixTests
    {
        private static readonly CodeFixProvider Fix = new MakeStaticFix();

        // ReSharper disable once InconsistentNaming
        private static readonly ExpectedDiagnostic CS0708 = ExpectedDiagnostic.Create("CS0708");

        [TestCase("public")]
        [TestCase("internal")]
        [TestCase("private")]
        public static void SimpleMethod(string modifier)
        {
            var before = @"
namespace N
{
    static class C
    {
        public void ↓M()
        {
        }
    }
}".AssertReplace("public", modifier);

            var after = @"
namespace N
{
    static class C
    {
        public static void M()
        {
        }
    }
}".AssertReplace("public", modifier);
            RoslynAssert.CodeFix(Fix, CS0708, before, after);
        }

        [Test]
        public static void ImplicitPrivate()
        {
            var before = @"
namespace N
{
    static class C
    {
        void ↓M()
        {
        }
    }
}";

            var after = @"
namespace N
{
    static class C
    {
        static void M()
        {
        }
    }
}";
            RoslynAssert.CodeFix(Fix, CS0708, before, after);
        }

        [Test]
        public static void TwoSimpleMethods()
        {
            var before = @"
namespace N
{
    public static class C
    {
        public void ↓M1()
        {
        }

        public void ↓M2()
        {
        }
    }
}";

            var after = @"
namespace N
{
    public static class C
    {
        public static void M1()
        {
        }

        public static void M2()
        {
        }
    }
}";
            RoslynAssert.FixAll(Fix, CS0708, before, after);
        }

        [Test]
        public static void AsyncMethod()
        {
            var before = @"
namespace N
{
    using System.Threading.Tasks;

    public static class C
    {
        public async Task ↓M() => await Task.CompletedTask;
    }
}";

            var after = @"
namespace N
{
    using System.Threading.Tasks;

    public static class C
    {
        public static async Task M() => await Task.CompletedTask;
    }
}";
            RoslynAssert.CodeFix(Fix, CS0708, before, after);
        }
    }
}
