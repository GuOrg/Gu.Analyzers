namespace Gu.Analyzers.Test.GU0074PreferPatternTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        public static class When
        {
            private static readonly DiagnosticAnalyzer Analyzer = new WhenAnalyzer();

            [Ignore("tbd")]
            [Test]
            public static void SwitchExpression()
            {
                var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type)
        {
            return type switch
            {
                { IsPublic: true } t when ↓t.IsAbstract => true,
                _ => false,
            };
        }
    }
}";

                var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type)
        {
            return type switch
            {
                { IsPublic: true, IsAbstract: true } => true,
                _ => false,
            };
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, before, after);
            }
        }
    }
}
