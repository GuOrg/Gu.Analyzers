namespace Gu.Analyzers.Test.GU0060EnumMemberValueConflictsWithAnotherTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly GU0060EnumMemberValueConflictsWithAnother Analyzer = new GU0060EnumMemberValueConflictsWithAnother();

        [Test]
        public void ExplicitAlias()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    [Flags]
    public enum Good
    {
        A = 1,
        B = 2,
        Gooooood = B
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ExplicitBitwiseOrSum()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    [Flags]
    public enum Good
    {
        A = 1,
        B = 2,
        Gooooood = A | B
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void SequentialNonFlagEnum()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public enum Bad
    {
        None,
        A,
        B,
        C
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AliasingEnumMembersNonFlag()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public enum Bad
    {
        None,
        A,
        B,
        C = B
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}