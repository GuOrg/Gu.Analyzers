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
            public static void SwitchStatementDeclarationPatternsUsesDesignation()
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
                case Type t when ↓t.IsAbstract:
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
                case Type { IsAbstract: true } t:
                    return true;
                default: return false;
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, before, after);
            }

            [Test]
            public static void SwitchStatementRecursivePatternsUsesDesignation()
            {
                var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type)
        {
            switch (type)
            {
                case { IsPublic: true } t when ↓t.IsAbstract:
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
        bool M(Type type)
        {
            switch (type)
            {
                case { IsPublic: true, IsAbstract: true } t:
                    return true;
                default: return false;
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, before, after);
            }

            [Test]
            public static void SwitchStatementUsesExpression()
            {
                var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type)
        {
            switch (type)
            {
                case { IsPublic: true } when ↓type.IsAbstract:
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
        bool M(Type type)
        {
            switch (type)
            {
                case { IsPublic: true, IsAbstract: true }:
                    return true;
                default: return false;
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, before, after);
            }

            [Test]
            public static void SwitchExpressionSingleLineUsesDesignation()
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
            public static void SwitchExpressionSingleLineUsesExpression()
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
                { IsPublic: true } when ↓type.IsAbstract => true,
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

            [Test]
            public static void LeftIsTypeRightIsType()
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
                { ReflectedType: Type reflectedType } when ↓reflectedType.Name is string name => true,
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
                { ReflectedType: Type { Name: string name } reflectedType } => true,
                _ => false,
            };
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "{ Name: string name }");
            }
        }
    }
}
