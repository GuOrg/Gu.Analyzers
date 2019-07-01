namespace Gu.Analyzers.Test.GU0011DontIgnoreReturnValueTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Diagnostics
    {
        private static readonly GU0011DoNotIgnoreReturnValue Analyzer = new GU0011DoNotIgnoreReturnValue();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0011DoNotIgnoreReturnValue.DiagnosticId);

        [TestCase("ints.Select(x => x);")]
        [TestCase("ints.Select(x => x).Where(x => x > 1);")]
        [TestCase("ints.Where(x => x > 1);")]
        public static void Linq(string linq)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Linq;
    class Foo
    {
        void Bar()
        {
            var ints = new[] { 1, 2, 3 };
            ↓ints.Select(x => x);
        }
    }
}".AssertReplace("ints.Select(x => x);", linq);

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic.WithMessage("Don't ignore the return value."), testCode);
        }

        [Test]
        public static void StringBuilderWriteLineToString()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Text;
    public class Foo
    {
        private int value;

        public void Bar()
        {
            var sb = new StringBuilder();
            ↓sb.AppendLine(""test"").ToString();
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [TestCase("Add(1)")]
        [TestCase("Remove(1)")]
        public static void ImmutableArray(string call)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Immutable;

    class Foo
    {
        public Foo(ImmutableArray<int> values)
        {
            ↓values.Add(1);
        }
    }
}".AssertReplace("Add(1)", call);

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [TestCase("Add(1)")]
        [TestCase("Remove(1)")]
        public static void ImmutableList(string call)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Immutable;

    class Foo
    {
        public Foo(ImmutableList<int> values)
        {
            ↓values.Add(1);
        }
    }
}".AssertReplace("Add(1)", call);

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Explicit("Fix later.")]
        [Test]
        public static void MoqSetupNonVoidNoReturn()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using Moq;
    using NUnit.Framework;

    public class Foo
    {
        [Test]
        public void Test()
        {
            var mock = new Mock<IFormatProvider>();
            ↓mock.Setup(x => x.GetFormat(It.IsAny<Type>()));
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
