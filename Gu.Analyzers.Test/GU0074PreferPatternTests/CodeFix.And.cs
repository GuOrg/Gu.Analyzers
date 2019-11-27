namespace Gu.Analyzers.Test.GU0074PreferPatternTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static partial class CodeFix
    {
        private static readonly CodeFixProvider Fix = new MoveToPatternFix();

        public static class And
        {
            private static readonly DiagnosticAnalyzer Analyzer = new BinaryExpressionAnalyzer();

            [Test]
            public static void LeftTrue()
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
                RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "type is { IsPublic: true }");
            }

            [Test]
            public static void LeftTrueMultiLine()
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
                RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "type is { IsPublic: true }");
            }

            [Test]
            public static void LeftNegated()
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
                RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "type is { IsPublic: false }");

                after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => !type.IsPublic && type is { IsAbstract: true };
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "type is { IsAbstract: true }");
            }

            [Test]
            public static void LeftString()
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
                RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "type is { Name: \"abc\" }");
            }

            [Test]
            public static void LeftNull()
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
                RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "type is { Name: null }");
            }

            [Test]
            public static void LeftIsNull()
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
                RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "type is { Name: null }");

                after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type.Name is null && type is { IsAbstract: true };
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "type is { IsAbstract: true }");
            }

            [Test]
            public static void LeftNotNull()
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
                RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "type is { Name: { } }");
            }

            [Test]
            public static void RightTrue()
            {
                var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { IsPublic: true } && ↓type.IsAbstract && ↓type.Name == ""abc"";
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
                RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: ", IsAbstract: true");
            }

            [Test]
            public static void RightTrueWhenLeftIsPattern()
            {
                var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(object o) => o is Type type && ↓type.IsAbstract;
    }
}";

                var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(object o) => o is Type { IsAbstract: true } type;
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "{ IsAbstract: true }");
            }

            [Test]
            public static void RightTrueWhenLeftIsRecursivePattern()
            {
                var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(object o) => o is Type { IsPublic: true } type && ↓type.IsAbstract && ↓type.Name == ""abc"";
    }
}";

                var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(object o) => o is Type { IsPublic: true, IsAbstract: true } type && type.Name == ""abc"";
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: ", IsAbstract: true");
            }

            [Test]
            public static void RightTrueLast()
            {
                var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { IsPublic: true } && ↓type.IsAbstract;
    }
}";

                var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { IsPublic: true, IsAbstract: true };
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, before, after);
            }

            [Test]
            public static void RightNotNullLast()
            {
                var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { IsPublic: true } && ↓type.Name != null;
    }
}";

                var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { IsPublic: true, Name: { } };
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, before, after);
            }

            [Test]
            public static void ExpressionBetween()
            {
                var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type, bool b) => type is { IsPublic: true } && b && ↓type.IsAbstract;
    }
}";

                var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type, bool b) => type is { IsPublic: true, IsAbstract: true } && b;
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, before, after);
            }

            [Test]
            public static void RightTrueMultiLine()
            {
                var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { IsPublic: true } &&
                             ↓type.IsAbstract &&
                             ↓type.Name == ""abc"";
    }
}";

                var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { IsPublic: true, IsAbstract: true } &&
                             type.Name == ""abc"";
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: ", IsAbstract: true");

                after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { IsPublic: true, Name: ""abc"" } &&
                             type.IsAbstract;
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: ", Name: \"abc\"");
            }

            [Test]
            public static void RightNegated()
            {
                var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { IsPublic: true } && ↓!type.IsAbstract;
    }
}";

                var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { IsPublic: true, IsAbstract: false };
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: ", IsAbstract: false");
            }

            [Test]
            public static void RightNegatedNotLast()
            {
                var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { IsPublic: true } && ↓!type.IsAbstract && ↓type.Name == ""abc"";
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
                RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: ", IsAbstract: false");
            }

            [Test]
            public static void RightString()
            {
                var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type is { IsPublic: true } && ↓type.Name == ""abc"" && ↓type.IsAbstract;
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
                RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: ", Name: \"abc\"");
            }

            [Test]
            public static void WhenAndSwitchExpression()
            {
                var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(object o) => o switch
        {
            Type t when ↓t.IsAbstract && ↓t.IsPublic => true,
            _ => false,
        };
    }
}";

                var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(object o) => o switch
        {
            Type { IsAbstract: true } t when t.IsPublic => true,
            _ => false,
        };
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "{ IsAbstract: true }");

                after = @"
namespace N
{
    using System;

    class C
    {
        bool M(object o) => o switch
        {
            Type { IsPublic: true } t when t.IsAbstract => true,
            _ => false,
        };
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "{ IsPublic: true }");
            }

            [Test]
            public static void WhenAndSwitchStatement()
            {
                var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(object o)
        {
            switch (o)
            {
                case Type t when ↓t.IsAbstract && ↓t.IsPublic:
                    return true;
                default: return false;
            }
        }
    }
}";

                var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(object o)
        {
            switch (o)
            {
                case Type { IsAbstract: true } t when t.IsPublic:
                    return true;
                default: return false;
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "{ IsAbstract: true }");

                after = @"
namespace N
{
    using System;

    class C
    {
        bool M(object o)
        {
            switch (o)
            {
                case Type { IsPublic: true } t when t.IsAbstract:
                    return true;
                default: return false;
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "{ IsPublic: true }");
            }
        }
    }
}
