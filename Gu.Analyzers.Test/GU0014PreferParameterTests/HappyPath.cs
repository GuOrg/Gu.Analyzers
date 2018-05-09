namespace Gu.Analyzers.Test.GU0014PreferParameterTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly ConstructorAnalyzer Analyzer = new ConstructorAnalyzer();

        [Test]
        public void SimpleAssign()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly string text;

        public Foo(string text)
        {
            this.text = text;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AssignInIfElse()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly string text;

        public Foo(string text)
        {
            if (text == null)
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ParameterAsArgument()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly string text;

        public Foo(string text)
        {
            this.text = text;
            var length = Meh(text.ToString());
        }

        private int Meh(string text) => text.Length;
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoreNameof()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(string text)
        {
            this.Text = text;
            var name = Id(nameof(this.Text));
        }

        public string Text { get; }

        private static string Id(string text) => text;
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoreWhenSideEffect()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(string text)
        {
            this.Text = text.ToLower();
            var temp = this.Text;
        }

        public string Text { get; }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoreWhenAssignedWithAddition()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int value)
        {
            this.Value = value + 1;
            var temp = this.Value;
        }

        public int Value { get; }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoreMutableInLambda()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo(string text)
        {
            this.Text = text;
            System.Console.CancelKeyPress += (_, __) => Console.WriteLine(this.Text);
        }

        public string Text { get; set; }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
