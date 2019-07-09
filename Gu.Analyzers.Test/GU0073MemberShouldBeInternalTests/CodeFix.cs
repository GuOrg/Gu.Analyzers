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

        [TestCase("readonly int F;")]
        [TestCase("static readonly int F;")]
        [TestCase("C() { }")]
        [TestCase("event Action E;")]
        [TestCase("int P { get; }")]
        [TestCase("void M() { }")]
        [TestCase("enum E { }")]
        [TestCase("struct S { }")]
        [TestCase("class Nested { }")]
        public static void InternalClass(string member)
        {
            var before = @"
namespace RoslynSandbox
{
    using System;

    internal class C
    {
        ↓public readonly int F;
    }
}".AssertReplace("readonly int F;", member);

            var after = @"
namespace RoslynSandbox
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
