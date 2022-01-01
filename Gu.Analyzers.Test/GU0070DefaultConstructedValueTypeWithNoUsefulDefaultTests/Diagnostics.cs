namespace Gu.Analyzers.Test.GU0070DefaultConstructedValueTypeWithNoUsefulDefaultTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class Diagnostics
{
    private static readonly GU0070DefaultConstructedValueTypeWithNoUsefulDefault Analyzer = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0070DefaultConstructedValueTypeWithNoUsefulDefault);

    [TestCase("new Guid()")]
    [TestCase("new DateTime()")]
    public static void When(string expression)
    {
        var code = @"
namespace N
{
    using System;

    public class C
    {
        public C()
        {
#pragma warning disable CS0219
            var unused = ↓new Guid();
        }
    }
}".AssertReplace("new Guid()", expression);
        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
    }
}