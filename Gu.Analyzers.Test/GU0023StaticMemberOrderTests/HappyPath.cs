namespace Gu.Analyzers.Test.GU0023StaticMemberOrderTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly DiagnosticAnalyzer Analyzer = new GU0023StaticMemberOrderAnalyzer();

        [Test]
        public void StaticFieldInitializedWithField()
        {
            var code = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public static readonly int Value1 = 1;

        public static readonly int Value2 = Value1;
    }
}";
            AnalyzerAssert.Valid(Analyzer, code);
        }

        [Test]
        public void ConstFieldInitializedWithField()
        {
            var code = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public const int Value1 = 1;

        public const int Value2 = Value1;
    }
}";
            AnalyzerAssert.Valid(Analyzer, code);
        }

        [Test]
        public void FieldInitializedWithFuncUsingField()
        {
            var code = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public static readonly Func<int> Value1 = () => Value2;

        public static readonly int Value2 = 2;
    }
}";
            AnalyzerAssert.Valid(Analyzer, code);
        }

        [Test]
        public void FieldInitializedWithStaticProperty()
        {
            var code = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public static readonly DateTime DateTime = DateTime.MaxValue;
    }
}";
            AnalyzerAssert.Valid(Analyzer, code);
        }

        [Test]
        public void ExcludeNameof()
        {
            var code = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public static readonly string Value1 = nameof(Value2);
        public static readonly string Value2 = ""2"";
    }
}";
            AnalyzerAssert.Valid(Analyzer, code);
        }

        [Test]
        public void UninitializedField()
        {
            var code = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public static int Value1;
    }
}";
            AnalyzerAssert.Valid(Analyzer, code);
        }

        [Test]
        public void UninitializedProperty()
        {
            var code = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public static int Value2 { get; }
    }
}";
            AnalyzerAssert.Valid(Analyzer, code);
        }

        [Test]
        public void FieldInitializedWithExpressionBodyProperty()
        {
            var code = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public static readonly int Value1 = Value2;

        public static int Value2 => 2;
    }
}";
            AnalyzerAssert.Valid(Analyzer, code);
        }

        [Test]
        public void DependencyPropertyRegisterReadOnly()
        {
            var code = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        private static readonly DependencyPropertyKey ValuePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(Value),
            typeof(int),
            typeof(FooControl), 
            new PropertyMetadata(default(int)));

        public static readonly DependencyProperty ValueProperty = ValuePropertyKey.DependencyProperty;

        public int Value
        {
            get => (int) this.GetValue(ValueProperty);
            private set => this.SetValue(ValuePropertyKey, value);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, code);
        }
    }
}
