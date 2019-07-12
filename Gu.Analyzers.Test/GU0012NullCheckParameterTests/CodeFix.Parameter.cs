namespace Gu.Analyzers.Test.GU0012NullCheckParameterTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static partial class CodeFix
    {
        internal static class Parameter
        {
            private static readonly DiagnosticAnalyzer Analyzer = new ParameterAnalyzer();
            private static readonly CodeFixProvider Fix = new NullCheckParameterFix();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0012NullCheckParameter.Descriptor);

            [TestCase("public")]
            [TestCase("internal")]
            [TestCase("protected")]
            public static void SimpleAssignFullyQualified(string access)
            {
                var before = @"
namespace RoslynSandbox
{
    public class C
    {
        private readonly string text;

        public C(string ↓text)
        {
            this.text = text;
        }
    }
}".AssertReplace("public C", $"{access} C");

                var after = @"
namespace RoslynSandbox
{
    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = text ?? throw new System.ArgumentNullException(nameof(text));
        }
    }
}".AssertReplace("public C", $"{access} C");

                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void SimpleAssignWhenUsingExists()
            {
                var before = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        private readonly string text;

        public C(string ↓text)
        {
            this.text = text;
        }
    }
}";

                var after = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = text ?? throw new ArgumentNullException(nameof(text));
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void PassAsArgument()
            {
                var before = @"
namespace RoslynSandbox
{
    public class C
    {
        private readonly int bar;

        public C(string ↓text)
        {
            this.bar = Bar(text);
        }

        private int Bar(string text) => text.Length;
    }
}";

                var after = @"
namespace RoslynSandbox
{
    public class C
    {
        private readonly int bar;

        public C(string text)
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void WhenNotUsed()
            {
                var before = @"
namespace RoslynSandbox
{
    public class C
    {
        public C(string text)
        {
        }
    }
}";

                var after = @"
namespace RoslynSandbox
{
    public class C
    {
        public C(string text)
        {
            if (text == null)
            {
                throw new System.ArgumentNullException(nameof(text));
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AfterOtherParameter()
            {
                var before = @"
namespace RoslynSandbox
{
    public sealed class C
    {
        public C(string s1, string ↓s2)
        {
            if (s1 == null)
            {
                throw new System.ArgumentNullException(nameof(s1));
            }
        }
    }
}";

                var after = @"
namespace RoslynSandbox
{
    public sealed class C
    {
        public C(string s1, string s2)
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void BeforeOtherParameter()
            {
                var before = @"
namespace RoslynSandbox
{
    public sealed class C
    {
        public C(string ↓s1, string s2)
        {
            if (s2 == null)
            {
                throw new System.ArgumentNullException(nameof(s2));
            }
        }
    }
}";

                var after = @"
namespace RoslynSandbox
{
    public sealed class C
    {
        public C(string s1, string s2)
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
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void FixAll()
            {
                var before = @"
namespace RoslynSandbox
{
    public sealed class C
    {
        public C(string ↓s1, string ↓s2)
        {
        }
    }
}";

                var after = @"
namespace RoslynSandbox
{
    public sealed class C
    {
        public C(string s1, string s2)
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
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void OutParameter()
            {
                var before = @"
namespace RoslynSandbox
{
    public sealed class C
    {
        private readonly string s1;

        public C(string ↓s1, out string s2)
        {
            this.s1 = s1;
            s2 = s1;
        }
    }
}";

                var after = @"
namespace RoslynSandbox
{
    public sealed class C
    {
        private readonly string s1;

        public C(string s1, out string s2)
        {
            this.s1 = s1 ?? throw new System.ArgumentNullException(nameof(s1));
            s2 = s1;
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }
        }
    }
}
