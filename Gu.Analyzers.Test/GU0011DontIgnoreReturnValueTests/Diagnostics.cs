namespace Gu.Analyzers.Test.GU0011DontIgnoreReturnValueTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        private static readonly GU0011DontIgnoreReturnValue Analyzer = new GU0011DontIgnoreReturnValue();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0011");

        [TestCase("ints.Select(x => x);")]
        [TestCase("ints.Select(x => x).Where(x => x > 1);")]
        [TestCase("ints.Where(x => x > 1);")]
        public void Linq(string linq)
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
}";
            testCode = testCode.AssertReplace("ints.Select(x => x);", linq);

            var expectedDiagnostic = ExpectedDiagnostic.CreateFromCodeWithErrorsIndicated(
                diagnosticId: "GU0011",
                message: "Don't ignore the return value.",
                code: testCode,
                cleanedSources: out testCode);
            AnalyzerAssert.Diagnostics(Analyzer, expectedDiagnostic, testCode);
        }

        [Test]
        public void StringBuilderWriteLineToString()
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
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [TestCase("Add(1)")]
        [TestCase("Remove(1)")]
        public void ImmutableArray(string call)
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
}";
            testCode = testCode.AssertReplace("Add(1)", call);
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [TestCase("Add(1)")]
        [TestCase("Remove(1)")]
        public void ImmutableList(string call)
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
}";
            testCode = testCode.AssertReplace("Add(1)", call);
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Explicit("Fix later.")]
        [Test]
        public void MoqSetupNonVoidNoReturn()
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
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
