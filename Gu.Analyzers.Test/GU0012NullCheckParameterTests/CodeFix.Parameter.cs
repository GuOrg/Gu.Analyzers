namespace Gu.Analyzers.Test.GU0012NullCheckParameterTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        internal class Parameter
        {
            private static readonly DiagnosticAnalyzer Analyzer = new ParameterAnalyzer();
            private static readonly CodeFixProvider Fix = new NullCheckParameterFix();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0012");

            [TestCase("public")]
            [TestCase("internal")]
            [TestCase("protected")]
            public void SimpleAssignFullyQualified(string access)
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly string text;

        public Foo(string ↓text)
        {
            this.text = text;
        }
    }
}".AssertReplace("public Foo", $"{access} Foo");

                var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly string text;

        public Foo(string text)
        {
            this.text = text ?? throw new System.ArgumentNullException(nameof(text));
        }
    }
}".AssertReplace("public Foo", $"{access} Foo");

                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void SimpleAssignWhenUsingExists()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly string text;

        public Foo(string ↓text)
        {
            this.text = text;
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly string text;

        public Foo(string text)
        {
            this.text = text ?? throw new ArgumentNullException(nameof(text));
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void PassAsArgument()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int bar;

        public Foo(string ↓text)
        {
            this.bar = Bar(text);
        }

        private int Bar(string text) => text.Length;
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int bar;

        public Foo(string text)
        {
            if (text == null)
            {
                throw new System.ArgumentNullException(nameof(text));
            }

            this.bar = Bar(text);
        }

        private int Bar(string text) => text.Length;
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void WhenNotUsed()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(string text)
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(string text)
        {
            if (text == null)
            {
                throw new System.ArgumentNullException(nameof(text));
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void AfterOtherParameter()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        public Foo(string s1, string ↓s2)
        {
            if (s1 == null)
            {
                throw new System.ArgumentNullException(nameof(s1));
            }
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        public Foo(string s1, string s2)
        {
            if (s1 == null)
            {
                throw new System.ArgumentNullException(nameof(s1));
            }

            if (s2 == null)
            {
                throw new System.ArgumentNullException(nameof(s2));
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void BeforeOtherParameter()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        public Foo(string ↓s1, string s2)
        {
            if (s2 == null)
            {
                throw new System.ArgumentNullException(nameof(s2));
            }
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        public Foo(string s1, string s2)
        {
            if (s1 == null)
            {
                throw new System.ArgumentNullException(nameof(s1));
            }

            if (s2 == null)
            {
                throw new System.ArgumentNullException(nameof(s2));
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }

            [Test]
            public void FixAll()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        public Foo(string ↓s1, string ↓s2)
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        public Foo(string s1, string s2)
        {
            if (s1 == null)
            {
                throw new System.ArgumentNullException(nameof(s1));
            }

            if (s2 == null)
            {
                throw new System.ArgumentNullException(nameof(s2));
            }
        }
    }
}";
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            }
        }
    }
}
