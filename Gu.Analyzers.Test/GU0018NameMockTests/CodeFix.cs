namespace Gu.Analyzers.Test.GU0018NameMockTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class CodeFix
{
    private static readonly VariableDeclaratorAnalyzer Analyzer = new();
    private static readonly RenameFix Fix = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0018aNameMock);

    [Test]
    public static void Local()
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
}";

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
    public static void LocalGeneric()
    {
        var iPlcOfT = @"
namespace N
{
    public interface IPlc<T>
    {
    }
}";

        var before = @"
namespace N
{
    using System;
    using Moq;
    using NUnit.Framework;

    public class C
    {
        [Test]
        public void M()
        {
            var ↓wrongMock = new Mock<IPlc<DateTime>>(MockBehavior.Strict);
        }
    }
}";

        var after = @"
namespace N
{
    using System;
    using Moq;
    using NUnit.Framework;

    public class C
    {
        [Test]
        public void M()
        {
            var plcOfDateTimeMock = new Mock<IPlc<DateTime>>(MockBehavior.Strict);
        }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { iPlcOfT, before }, after: after);
    }

    [Test]
    public static void OneLocalMock()
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
            var ↓mock = new Mock<IPlc>(MockBehavior.Strict);
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
        [Test]
        public void M()
        {
            var plcMock = new Mock<IPlc>(MockBehavior.Strict);
        }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic.Create(Descriptors.GU0018bNameMock), new[] { iPlc, before }, after: after);
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
        private Mock<IPlc>? ↓_wrongMock;

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
        private Mock<IPlc>? _plcMock;

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
