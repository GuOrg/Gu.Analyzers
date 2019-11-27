namespace Gu.Analyzers.Test.GU0074PreferPatternTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        private static readonly CodeFixProvider Fix = new PatternFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0074PreferPattern);

        public static class And
        {
            private static readonly DiagnosticAnalyzer Analyzer = new BinaryExpressionAnalyzer();

            [Test]
            public static void True()
            {
                var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => ↓type.IsPublic && ↓type.IsAbstract;
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "type is { IsPublic: true }");
            }

            [Test]
            public static void TrueMultiLine()
            {
                var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => ↓type.IsPublic &&
                             ↓type.IsAbstract;
    }
}";

                var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { IsPublic: true } &&
                             type.IsAbstract;
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "type is { IsPublic: true }");
            }

            [Test]
            public static void Negated()
            {
                var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => ↓!type.IsPublic && ↓type.IsAbstract;
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "type is { IsPublic: false }");

                after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => !type.IsPublic && type is { IsAbstract: true };
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "type is { IsAbstract: true }");
            }

            [Test]
            public static void String()
            {
                var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => ↓type.Name == ""abc"" && ↓type.IsAbstract;
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "type is { Name: \"abc\" }");
            }

            [Test]
            public static void Null()
            {
                var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => ↓type.Name == null && ↓type.IsAbstract;
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "type is { Name: null }");
            }

            [Test]
            public static void IsNull()
            {
                var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => ↓type.Name is null && ↓type.IsAbstract;
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "type is { Name: null }");

                after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type.Name is null && type is { IsAbstract: true };
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "type is { IsAbstract: true }");
            }

            [Test]
            public static void NotNull()
            {
                var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => ↓type.Name != null && ↓type.IsAbstract;
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "type is { Name: { } }");
            }
        }
    }
}
