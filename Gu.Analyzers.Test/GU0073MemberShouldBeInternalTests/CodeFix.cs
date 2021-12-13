namespace Gu.Analyzers.Test.GU0073MemberShouldBeInternalTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new GU0073MemberShouldBeInternal();
        private static readonly CodeFixProvider Fix = new MakeInternalFix();

        [Test]
        public static void Messages()
        {
            var before = @"
#pragma warning disable CS0649
namespace N
{
    using System;

    internal class C
    {
        ↓public readonly int F;
    }
}";

            var after = @"
#pragma warning disable CS0649
namespace N
{
    using System;

    internal class C
    {
        internal readonly int F;
    }
}";
            var expectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0073MemberShouldBeInternal).WithMessage("Member F of non-public type C should be internal");
            RoslynAssert.CodeFix(Analyzer, Fix, expectedDiagnostic, before, after, fixTitle: "Make internal.");
        }

        [TestCase("readonly int F;")]
        [TestCase("static readonly int F;")]
        [TestCase("C() { }")]
        [TestCase("event Action? E;")]
        [TestCase("int P { get; }")]
        [TestCase("void M() { }")]
        [TestCase("enum E { }")]
        [TestCase("struct S { }")]
        [TestCase("class Nested { }")]
        public static void InternalClass(string member)
        {
            var before = @"
#pragma warning disable CS0067, CS0649
namespace N
{
    using System;

    internal class C
    {
        ↓public readonly int F;
    }
}".AssertReplace("readonly int F;", member);

            var after = @"
#pragma warning disable CS0067, CS0649
namespace N
{
    using System;

    internal class C
    {
        internal readonly int F;
    }
}".AssertReplace("readonly int F;", member);
            RoslynAssert.CodeFix(Analyzer, Fix, before, after);
        }
    }
}
