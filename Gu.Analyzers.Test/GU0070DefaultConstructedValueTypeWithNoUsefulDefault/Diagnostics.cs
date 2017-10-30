namespace Gu.Analyzers.Test.GU0070DefaultConstructedValueTypeWithNoUsefulDefault
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        [Test]
        public void UselessDefaultGuid()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class A
    {
        public void F()
        {
            var g = ↓new System.Guid();
        }
    }
}";
            AnalyzerAssert.Diagnostics<Analyzers.GU0070DefaultConstructedValueTypeWithNoUsefulDefault>(testCode);
        }
    }
}