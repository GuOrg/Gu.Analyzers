namespace Gu.Analyzers.Test.GU0018NameMockTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class Valid
    {
        private static readonly DiagnosticAnalyzer Analyzer = new VariableDeclaratorAnalyzer();

        [Test]
        public static void LocalSuffixed()
        {
            var iPlc = @"
namespace N
{
    public interface IPlc
    {
    }
}";

            var code = @"
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
            RoslynAssert.Valid(Analyzer, iPlc, code);
        }

        [Test]
        public static void LocalGeneric()
        {
            var iPlc = @"
namespace N
{
    public interface IPlc
    {
    }
}";

            var code = @"
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
            RoslynAssert.Valid(Analyzer, iPlc, code);
        }

        [Test]
        public static void FieldSuffixed()
        {
            var iPlc = @"
namespace N
{
    public interface IPlc
    {
    }
}";

            var code = @"
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
            RoslynAssert.Valid(Analyzer, iPlc, code);
        }
    }
}
