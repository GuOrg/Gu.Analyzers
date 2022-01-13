namespace Gu.Analyzers.Test.GU0014PreferParameterTests;

using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

internal static class Valid
{
    private static readonly ConstructorAnalyzer Analyzer = new();
    private static readonly DiagnosticDescriptor Descriptor = Descriptors.GU0014PreferParameter;

    [Test]
    public static void SimpleAssign()
    {
        var code = @"
namespace N
{
    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = text;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void AssignWithExpression()
    {
        var code = @"
namespace N
{
    using System;

    public class C
    {
        public C(int value)
        {
            this.Square = value * value;
            if (this.Square > 10)
            {
                throw new ArgumentException(nameof(value));
            }
        }

        public int Square { get; }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void AssignedTwiceWithDifferent()
    {
        var testCode = @"
namespace N
{
    using System;

    public class C
    {
        public C(int value1, int value2)
        {
            this.Bar = value1;
            this.Bar = value2;
            if (this.Bar > 10)
            {
                throw new ArgumentException(nameof(value1));
            }
        }

        public int Bar { get; }
    }
}";
        RoslynAssert.Valid(Analyzer, Descriptor, testCode);
    }

    [Test]
    public static void SecondAssignmentWithExpression()
    {
        var code = @"
namespace N
{
    using System;

    public class C
    {
        public C(int bar)
        {
            this.Bar = bar;
            this.Bar = bar * 2;
            if (this.Bar > 10)
            {
                throw new ArgumentException(nameof(bar));
            }
        }

        public int Bar { get; }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void SecondAssignmentWithExpressionRepro()
    {
        var testCode = @"
namespace N
{
    using System;

    public class C
    {
        public C(int value1, int value2)
        {
            this.Bar = value1;
            this.Bar = value2 * 2;
            if (this.Bar > 10)
            {
                throw new ArgumentException(nameof(value1));
            }
        }

        public int Bar { get; }
    }
}";
        RoslynAssert.Valid(Analyzer, Descriptor, testCode);
    }

    [Test]
    public static void UsedBeforeAssign()
    {
        var code = @"
namespace N
{
    public class C
    {
        private readonly string text;

        public C(string text)
        {
            var temp = this.text;
            this.text = text;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void AssignInIfElse()
    {
        var code = @"
namespace N
{
    public class C
    {
        private readonly string text;

        public C(string text)
        {
            if (text is null)
            {
                this.text = string.Empty;
            }
            else
            {
                this.text = text;
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ParameterAsArgument()
    {
        var code = @"
namespace N
{
    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = text;
            var length = Meh(text.ToString());
        }

        private int Meh(string text) => text.Length;
    }
}";

        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void IgnoreNameof()
    {
        var code = @"
namespace N
{
    public class C
    {
        public C(string text)
        {
            this.Text = text;
            var name = Id(nameof(this.Text));
        }

        public string Text { get; }

        private static string Id(string text) => text;
    }
}";

        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void IgnoreWhenSideEffect()
    {
        var code = @"
namespace N
{
    public class C
    {
        public C(string text)
        {
            this.Text = text.ToLower();
            var temp = this.Text;
        }

        public string Text { get; }
    }
}";

        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void IgnoreWhenAssignedWithAddition()
    {
        var code = @"
namespace N
{
    public class C
    {
        public C(int value)
        {
            this.Value = value + 1;
            var temp = this.Value;
        }

        public int Value { get; }
    }
}";

        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void IgnoreMutableInLambda()
    {
        var code = @"
namespace N
{
    using System;

    public class C
    {
        public C(string text)
        {
            this.Text = text;
            System.Console.CancelKeyPress += (_, __) => Console.WriteLine(this.Text);
        }

        public string Text { get; set; }
    }
}";

        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void BaseConstructorCall()
    {
        var c1 = @"
namespace N
{
    public class C1
    {
        public C1(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }
    }
}";

        var c2 = @"
namespace N
{
    public class C2 : C1
    {
        public C2(int a, int b, int c, int d)
            : base(a, b, c, d)
        {
        }
    }
}";
        RoslynAssert.Valid(Analyzer, c1, c2);
    }

    [Test]
    public static void BaseConstructorCallSimple()
    {
        var c1 = @"
namespace N
{
    public class C1
    {
        public C1(int a)
        {
            this.A = a;
        }

        public int A { get; }
    }
}";

        var c2 = @"
namespace N
{
    public class C2 : C1
    {
        public C2(int a)
            : base(a)
        {
        }
    }
}";
        RoslynAssert.Valid(Analyzer, c1, c2);
    }
}
