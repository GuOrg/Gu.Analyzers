namespace Gu.Analyzers.Test.GU0018NameMockTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;

    using NUnit.Framework;

    internal static class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new VariableDeclaratorAnalyzer();
        private static readonly CodeFixProvider Fix = new RenameFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0018NameMock);

        [TestCase("wrongMock")]
        public static void Local(string name)
        {
            var iPlc = @"
namespace N
{
    public interface IPlc
    {
    }
}";

            var before = @"
namespace N
{
    using Moq;
    using NUnit.Framework;

    public class C
    {
        [Test]
        public void M()
        {
            var ↓wrongMock = new Mock<IPlc>(MockBehavior.Strict);
        }
    }
}".AssertReplace("wrongMock", name);

            var after = @"
namespace N
{
    using Moq;
    using NUnit.Framework;

    public class C
    {
        [Test]
        public void M()
        {
            var plcMock = new Mock<IPlc>(MockBehavior.Strict);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { iPlc, before }, after: after);
        }

        [Test]
        public static void Field()
        {
            var iPlc = @"
namespace N
{
    public interface IPlc
    {
    }
}";

            var before = @"
namespace N
{
    using Moq;
    using NUnit.Framework;

    public class C
    {
        private Mock<IPlc> ↓_wrongMock;

        [SetUp]
        public void M()
        {
            _wrongMock = new Mock<IPlc>(MockBehavior.Strict);
        }
    }
}";

            var after = @"
namespace N
{
    using Moq;
    using NUnit.Framework;

    public class C
    {
        private Mock<IPlc> _plcMock;

        [SetUp]
        public void M()
        {
            _plcMock = new Mock<IPlc>(MockBehavior.Strict);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { iPlc, before }, after: after);
        }
    }
}
