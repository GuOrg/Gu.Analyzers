﻿namespace Gu.Analyzers.Test.GU0072AllTypesShouldBeInternalTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class CodeFix
{
    private static readonly GU0072AllTypesShouldBeInternal Analyzer = new GU0072AllTypesShouldBeInternal().DefaultEnabled();
    private static readonly MakeInternalFix Fix = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0072AllTypesShouldBeInternal);

    [Test]
    public static void Class()
    {
        var before = """
            namespace N;
            ↓public class C
            {
            }
            """;

        var after = """
            namespace N;
            internal class C
            {
            }
            """;
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }

    [Test]
    public static void Struct()
    {
        var before = """
            namespace N;
            ↓public struct S
            {
            }
            """;

        var after = """
            namespace N;
            internal struct S
            {
            }
            """;
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }
}
