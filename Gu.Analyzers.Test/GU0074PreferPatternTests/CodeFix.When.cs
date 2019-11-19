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

            [Test]
            public static void SwitchExpressionSingleLine()
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
                { IsPublic: true, IsAbstract: true } t => true,
                _ => false,
            };
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, before, after);
            }

            [Test]
            public static void SwitchExpressionWhenOnSeparateLine()
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
                { IsPublic: true } t
                    when ↓t.IsAbstract => true,
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
                { IsPublic: true, IsAbstract: true } t => true,
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
