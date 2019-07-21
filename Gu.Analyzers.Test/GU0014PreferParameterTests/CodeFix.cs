namespace Gu.Analyzers.Test.GU0014PreferParameterTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ConstructorAnalyzer();
        private static readonly CodeFixProvider Fix = new UseParameterFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0014PreferParameter);

        [Test]
        public static void WhenAccessingFieldProperty()
        {
            var before = @"
namespace N
{
    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = text;
            var length = ↓this.text.Length;
        }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = text;
            var length = text.Length;
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenAccessingFieldElvisProperty()
        {
            var before = @"
namespace N
{
    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = text;
            var length = ↓this.text?.Length;
        }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = text;
            var length = text?.Length;
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenAccessingFieldMethod()
        {
            var before = @"
namespace N
{
    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = text;
            var length = ↓this.text.ToString();
        }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = text;
            var length = text.ToString();
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenUsingPropertyProperty()
        {
            var before = @"
namespace N
{
    public class C
    {
        public C(string text)
        {
            this.Text = text;
            var length = ↓this.Text.Length;
        }

        public string Text { get; }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        public C(string text)
        {
            this.Text = text;
            var length = text.Length;
        }

        public string Text { get; }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenUsingPropertyMethod()
        {
            var before = @"
namespace N
{
    public class C
    {
        public C(string text)
        {
            this.Text = text;
            var length = ↓this.Text.ToString();
        }

        public string Text { get; }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        public C(string text)
        {
            this.Text = text;
            var length = text.ToString();
        }

        public string Text { get; }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenPassingFieldAsArgument()
        {
            var before = @"
namespace N
{
    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = text;
            var length = M(↓this.text);
        }

        private int M(string text) => text.Length;
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = text;
            var length = M(text);
        }

        private int M(string text) => text.Length;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenPassingFieldAsArgumentWhitespace()
        {
            var before = @"
namespace N
{
    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = text;
            var length = M(
                ↓this.text);
        }

        private int M(string text) => text.Length;
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = text;
            var length = M(
                text);
        }

        private int M(string text) => text.Length;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenPassingUnderscoreFieldAsArgumentWhitespace()
        {
            var before = @"
namespace N
{
    public class C
    {
        private readonly string _text;

        public C(string text)
        {
            _text = text;
            var length = M(
                ↓_text);
        }

        private int M(string text) => text.Length;
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private readonly string _text;

        public C(string text)
        {
            _text = text;
            var length = M(
                text);
        }

        private int M(string text) => text.Length;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenPassingUnderscoreFieldAsArgument()
        {
            var before = @"
namespace N
{
    public class C
    {
        private readonly string _text;

        public C(string text)
        {
            _text = text;
            var length = M(↓_text);
        }

        private int M(string text) => text.Length;
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private readonly string _text;

        public C(string text)
        {
            _text = text;
            var length = M(text);
        }

        private int M(string text) => text.Length;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void GetOnlyInLambda()
        {
            var before = @"
namespace N
{
    using System;

    public class C
    {
        public C(string text)
        {
            this.Text = text;
            System.Console.CancelKeyPress += (_, __) => Console.WriteLine(↓this.Text);
        }

        public string Text { get; }
    }
}";

            var after = @"
namespace N
{
    using System;

    public class C
    {
        public C(string text)
        {
            this.Text = text;
            System.Console.CancelKeyPress += (_, __) => Console.WriteLine(text);
        }

        public string Text { get; }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void LeftFieldInMultiplication()
        {
            var before = @"
namespace N
{
    public class C
    {
        private readonly int n;

        public C(int n)
        {
            this.n = n;
            var square = ↓this.n * n;
        }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private readonly int n;

        public C(int n)
        {
            this.n = n;
            var square = n * n;
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void RightFieldInMultiplication()
        {
            var before = @"
namespace N
{
    public class C
    {
        private readonly int n;

        public C(int n)
        {
            this.n = n;
            var square = n * ↓this.n;
        }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private readonly int n;

        public C(int n)
        {
            this.n = n;
            var square = n * n;
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }
    }
}
