namespace Gu.Analyzers.Test.GU0074PreferPatternTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new BinaryExpressionAnalyzer();
        private static readonly CodeFixProvider Fix = new MoveToPatternFix();

        [Test]
        public static void CreatePatternForLeft()
        {
            var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => ↓type.IsPublic && type.IsAbstract;
    }
}";

            var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { IsPublic: true } && type.IsAbstract;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "type is { IsPublic: true }");
        }

        [Test]
        public static void MergeRight()
        {
            var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { IsPublic: true } && ↓type.IsAbstract && type.Name == ""abc"";
    }
}";

            var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { IsPublic: true, IsAbstract: true } && type.Name == ""abc"";
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, before, after);
        }
    }
}
