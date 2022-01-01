namespace Gu.Analyzers.Test.GU0023StaticMemberOrderTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class Valid
{
    private static readonly GU0023StaticMemberOrderAnalyzer Analyzer = new();

    [Test]
    public static void StaticFieldInitializedWithField()
    {
        var code = @"
namespace N
{
    public class C
    {
        public static readonly int Value1 = 1;

        public static readonly int Value2 = Value1;
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ConstFieldInitializedWithField()
    {
        var code = @"
namespace N
{
    public class C
    {
        public const int Value1 = 1;

        public const int Value2 = Value1;
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void FieldInitializedWithFuncUsingField()
    {
        var code = @"
namespace N
{
    using System;

    public class C
    {
        public static readonly Func<int> Value1 = () => Value2;

        public static readonly int Value2 = 2;
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void FieldInitializedWithStaticProperty()
    {
        var code = @"
namespace N
{
    using System;

    public class C
    {
        public static readonly DateTime DateTime = DateTime.MaxValue;
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ExcludeNameof()
    {
        var code = @"
namespace N
{
    public class C
    {
        public static readonly string Value1 = nameof(Value2);
        public static readonly string Value2 = ""2"";
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void UninitializedField()
    {
        var code = @"
namespace N
{
    public class C
    {
        public static int Value1;
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void UninitializedProperty()
    {
        var code = @"
namespace N
{
    public class C
    {
        public static int Value2 { get; }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void FieldInitializedWithExpressionBodyProperty()
    {
        var code = @"
namespace N
{
    public class C
    {
        public static readonly int Value1 = Value2;

        public static int Value2 => 2;
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void DependencyPropertyRegisterReadOnly()
    {
        var code = @"
namespace N
{
    using System.Windows;
    using System.Windows.Controls;

    public class CControl : Control
    {
        private static readonly DependencyPropertyKey ValuePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(Value),
            typeof(int),
            typeof(CControl), 
            new PropertyMetadata(default(int)));

        public static readonly DependencyProperty ValueProperty = ValuePropertyKey.DependencyProperty;

        public int Value
        {
            get => (int) this.GetValue(ValueProperty);
            private set => this.SetValue(ValuePropertyKey, value);
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ExtensionMethod()
    {
        var ext = @"
namespace N
{
    public static class Ext
    {
        public static string M1(this string text) => text;

        public static string M2(this string text) => text;
    }
}";

        var code = @"
namespace N
{
    public class C
    {
        private static readonly string F = string.Empty.M1().M2();
    }
}";
        RoslynAssert.Valid(Analyzer, ext, code);
    }
}