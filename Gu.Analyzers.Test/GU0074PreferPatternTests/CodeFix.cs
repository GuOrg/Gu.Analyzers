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
        public static void CreatePatternForLeftWhenTrue()
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
        public static void CreatePatternForLeftWhenNegated()
        {
            var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => ↓!type.IsPublic && type.IsAbstract;
    }
}";

            var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { IsPublic: false } && type.IsAbstract;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "type is { IsPublic: false }");
        }

        [Test]
        public static void CreatePatternForLeftWhenString()
        {
            var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => ↓type.Name == ""abc"" && type.IsAbstract;
    }
}";

            var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { Name: ""abc"" } && type.IsAbstract;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "type is { Name: \"abc\" }");
        }

        [Test]
        public static void CreatePatternForLeftWhenNull()
        {
            var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => ↓type.Name == null && type.IsAbstract;
    }
}";

            var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { Name: null } && type.IsAbstract;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "type is { Name: null }");
        }

        [Test]
        public static void CreatePatternForLeftWhenIsNull()
        {
            var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => ↓type.Name is null && type.IsAbstract;
    }
}";

            var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { Name: null } && type.IsAbstract;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "type is { Name: null }");
        }

        [Ignore("tbd")]
        [Test]
        public static void CreatePatternForLeftWhenNotNull()
        {
            var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => ↓type.Name != null && type.IsAbstract;
    }
}";

            var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { Name: { } } && type.IsAbstract;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "type is { Name: null }");
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

        [Test]
        public static void MergeRightWhenNegated()
        {
            var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { IsPublic: true } && ↓!type.IsAbstract && type.Name == ""abc"";
    }
}";

            var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { IsPublic: true, IsAbstract: false } && type.Name == ""abc"";
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, before, after);
        }

        [Ignore("tbd")]
        [Test]
        public static void MergeStringRight()
        {
            var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { IsPublic: true } && ↓type.Name == ""abc"" && type.IsAbstract;
    }
}";

            var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { IsPublic: true, Name: ""abc"" } && type.IsAbstract;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, before, after);
        }
    }
}
