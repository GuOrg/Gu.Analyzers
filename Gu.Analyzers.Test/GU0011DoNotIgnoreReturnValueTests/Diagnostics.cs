namespace Gu.Analyzers.Test.GU0011DoNotIgnoreReturnValueTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Diagnostics
    {
        private static readonly GU0011DoNotIgnoreReturnValue Analyzer = new();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0011DoNotIgnoreReturnValue);

        [TestCase("ints.Select(x => x);")]
        [TestCase("ints.Select(x => x).Where(x => x > 1);")]
        [TestCase("ints.Where(x => x > 1);")]
        public static void Linq(string linq)
        {
            var code = @"
namespace N
{
    using System.Linq;

    class C
    {
        void M()
        {
            var ints = new[] { 1, 2, 3 };
            ↓ints.Select(x => x);
        }
    }
}".AssertReplace("ints.Select(x => x);", linq);

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic.WithMessage("Don't ignore the return value."), code);
        }

        [Test]
        public static void StringBuilderWriteLineToString()
        {
            var code = @"
namespace N
{
    using System.Text;
    public class C
    {
        private int value;

        public void M()
        {
            var sb = new StringBuilder();
            ↓sb.AppendLine(""test"").ToString();
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [TestCase("Add(1)")]
        [TestCase("Remove(1)")]
        public static void ImmutableArray(string call)
        {
            var code = @"
namespace N
{
    using System.Collections.Immutable;

    class C
    {
        public C(ImmutableArray<int> values)
        {
            ↓values.Add(1);
        }
    }
}".AssertReplace("Add(1)", call);

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [TestCase("Add(1)")]
        [TestCase("Remove(1)")]
        public static void ImmutableList(string call)
        {
            var code = @"
namespace N
{
    using System.Collections.Immutable;

    class C
    {
        public C(ImmutableList<int> values)
        {
            ↓values.Add(1);
        }
    }
}".AssertReplace("Add(1)", call);

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Ignore("Fix later.")]
        [Test]
        public static void MoqSetupNonVoidNoReturn()
        {
            var code = @"
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
            var mock = new Mock<IFormatProvider>();
            ↓mock.Setup(x => x.GetFormat(It.IsAny<Type>()));
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }
    }
}
