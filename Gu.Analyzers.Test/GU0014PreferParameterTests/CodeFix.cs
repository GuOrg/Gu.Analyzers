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
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0014PreferParameter.Descriptor);

        [Test]
        public static void WhenAccessingFieldProperty()
        {
            var before = @"
namespace N
{
    public class Foo
    {
        private readonly string text;

        public Foo(string text)
        {
            this.text = text;
            var length = ↓this.text.Length;
        }
    }
}";

            var after = @"
namespace N
{
    public class Foo
    {
        private readonly string text;

        public Foo(string text)
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
    public class Foo
    {
        private readonly string text;

        public Foo(string text)
        {
            this.text = text;
            var length = ↓this.text?.Length;
        }
    }
}";

            var after = @"
namespace N
{
    public class Foo
    {
        private readonly string text;

        public Foo(string text)
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
    public class Foo
    {
        private readonly string text;

        public Foo(string text)
        {
            this.text = text;
            var length = ↓this.text.ToString();
        }
    }
}";

            var after = @"
namespace N
{
    public class Foo
    {
        private readonly string text;

        public Foo(string text)
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
    public class Foo
    {
        public Foo(string text)
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
    public class Foo
    {
        public Foo(string text)
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
    public class Foo
    {
        public Foo(string text)
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
    public class Foo
    {
        public Foo(string text)
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
    public class Foo
    {
        private readonly string text;

        public Foo(string text)
        {
            this.text = text;
            var length = Meh(↓this.text);
        }

        private int Meh(string text) => text.Length;
    }
}";

            var after = @"
namespace N
{
    public class Foo
    {
        private readonly string text;

        public Foo(string text)
        {
            this.text = text;
            var length = Meh(text);
        }

        private int Meh(string text) => text.Length;
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
    public class Foo
    {
        private readonly string text;

        public Foo(string text)
        {
            this.text = text;
            var length = Meh(
                ↓this.text);
        }

        private int Meh(string text) => text.Length;
    }
}";

            var after = @"
namespace N
{
    public class Foo
    {
        private readonly string text;

        public Foo(string text)
        {
            this.text = text;
            var length = Meh(
                text);
        }

        private int Meh(string text) => text.Length;
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
    public class Foo
    {
        private readonly string _text;

        public Foo(string text)
        {
            _text = text;
            var length = Meh(
                ↓_text);
        }

        private int Meh(string text) => text.Length;
    }
}";

            var after = @"
namespace N
{
    public class Foo
    {
        private readonly string _text;

        public Foo(string text)
        {
            _text = text;
            var length = Meh(
                text);
        }

        private int Meh(string text) => text.Length;
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
    public class Foo
    {
        private readonly string _text;

        public Foo(string text)
        {
            _text = text;
            var length = Meh(↓_text);
        }

        private int Meh(string text) => text.Length;
    }
}";

            var after = @"
namespace N
{
    public class Foo
    {
        private readonly string _text;

        public Foo(string text)
        {
            _text = text;
            var length = Meh(text);
        }

        private int Meh(string text) => text.Length;
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

    public class Foo
    {
        public Foo(string text)
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

    public class Foo
    {
        public Foo(string text)
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
    public class Foo
    {
        private readonly int n;

        public Foo(int n)
        {
            this.n = n;
            var square = ↓this.n * n;
        }
    }
}";

            var after = @"
namespace N
{
    public class Foo
    {
        private readonly int n;

        public Foo(int n)
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
    public class Foo
    {
        private readonly int n;

        public Foo(int n)
        {
            this.n = n;
            var square = n * ↓this.n;
        }
    }
}";

            var after = @"
namespace N
{
    public class Foo
    {
        private readonly int n;

        public Foo(int n)
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
