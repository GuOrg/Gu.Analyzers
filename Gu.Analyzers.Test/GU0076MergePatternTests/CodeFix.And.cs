namespace Gu.Analyzers.Test.GU0076MergePatternTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

public static partial class CodeFix
{
    private static readonly PatternFix Fix = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0076MergePattern);

    public static class And
    {
        private static readonly BinaryExpressionAnalyzer Analyzer = new();

        [Test]
        public static void True()
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: ", IsAbstract: true");
        }

        [Test]
        public static void TrueWhenLeftIsPattern()
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "{ IsAbstract: true }");
        }

        [Test]
        public static void TrueWhenLeftIsRecursivePattern()
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: ", IsAbstract: true");
        }

        [Test]
        public static void TrueLast()
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void NotNullLast()
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void ExpressionBefore()
        {
            var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type, bool b) => b && type is { IsPublic: true } && ↓type.IsAbstract;
    }
}";

            var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type, bool b) => b && type is { IsPublic: true, IsAbstract: true };
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: ", IsAbstract: true");

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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: ", Name: \"abc\"");
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: ", IsAbstract: false");
        }

        [Test]
        public static void NegatedNotLast()
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: ", IsAbstract: false");
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: ", Name: \"abc\"");
        }

        [Test]
        public static void Enum()
        {
            var before = @"
namespace N
{
    using System;
    using System.Reflection;

    class C
    {
        bool M(Type type) => type is { IsPublic: true } && ↓type.MemberType == MemberTypes.NestedType && ↓type.IsAbstract;
    }
}";

            var after = @"
namespace N
{
    using System;
    using System.Reflection;

    class C
    {
        bool M(Type type) => type is { IsPublic: true, MemberType: MemberTypes.NestedType } && type.IsAbstract;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: ", MemberType: MemberTypes.NestedType");
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "{ IsAbstract: true }");

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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "{ IsPublic: true }");
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "{ IsAbstract: true }");

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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "{ IsPublic: true }");
        }

        [Test]
        public static void SwitchWhenLeftIsTypeDeclarationRightEquals()
        {
            var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(object o)
        {
            return o switch
            {
                Type type
                when ↓type.Name is string name && ↓name.Length == 5
                => true,
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
        bool M(object o)
        {
            return o switch
            {
                Type { Name: string name } type
                when name.Length == 5
                => true,
            _ => false,
            };
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "{ Name: string name }");
        }

        [Test]
        public static void SwitchWhenLeftIsTypeDeclarationRightEqualsAndTrue()
        {
            var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(object o)
        {
            return o switch
            {
                Type type
                when ↓type.Name is string name && ↓name.Length == 5 && ↓type.IsPublic
                => true,
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
        bool M(object o)
        {
            return o switch
            {
                Type { Name: string name } type
                when name.Length == 5 && type.IsPublic
                => true,
            _ => false,
            };
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "{ Name: string name }");
        }

        [Test]
        public static void LeftIsTypeDeclarationWhenRightIsPattern()
        {
            var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(object o)
        {
            return o switch
            {
                Type type
                when ↓type.Name is { } name && ↓name.Length == 5
                => true,
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
        bool M(object o)
        {
            return o switch
            {
                Type { Name: { } name } type
                when name.Length == 5
                => true,
            _ => false,
            };
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "{ Name: { } name }");
        }

        [Test]
        public static void LeftIsType()
        {
            var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type.Name is string name && ↓name.Length == 4;
    }
}";

            var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type.Name is string { Length: 4 } name;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "{ Length: 4 }");
        }

        [Test]
        public static void LeftIsEmptyPattern()
        {
            var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type.Name is { } name && ↓name.Length == 4;
    }
}";

            var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type.Name is { Length: 4 } name;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "{ Length: 4 }");
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
        bool M(Type type) => type.ReflectedType is Type reflectedType &&
                             ↓reflectedType.Name is string name;
    }
}";

            var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type.ReflectedType is Type { Name: string name } reflectedType;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "{ Name: string name }");
        }

        [Test]
        public static void MergeInRecursive()
        {
            var before = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type.ReflectedType is { Name: { } name } &&
                             ↓name.Length == 5;
    }
}";

            var after = @"
namespace N
{
    using System;

    class C
    {
        bool M(Type type) => type.ReflectedType is { Name: { Length: 5 } name };
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "{ Length: 5 }");
        }
    }
}
