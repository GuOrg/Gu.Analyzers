namespace Gu.Analyzers.Test.GU0023StaticMemberOrderTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly DiagnosticAnalyzer Analyzer = new GU0023StaticMemberOrderAnalyzer();

        [Test]
        public void FieldInitializedWithField()
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
    }
}
